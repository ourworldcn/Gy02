using AutoMapper;
using GuangYuan.GY001.TemplateDb;
using GY02.Base;
using GY02.Commands;
using GY02.Managers;
using GY02.Publisher;
using GY02.TemplateDb;
using GY02.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using OW.DDD;
using OW.Game;
using OW.Game.Conditional;
using OW.Game.Entity;
using OW.Game.Manager;
using OW.Game.Managers;
using OW.Game.PropertyChange;
using OW.Game.Store;
using OW.GameDb;
using OW.Server;
using OW.SyncCommand;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GY02
{
    /// <summary>
    /// 游戏世界的主服务。该服务退出表示游戏世界不再存在。
    /// </summary>
    public class GameHostedService : BackgroundService
    {
        public GameHostedService(IServiceProvider services, IHostApplicationLifetime applicationLifetime, ILogger<GameHostedService> logger)
        {
            _Services = services;
            _ApplicationLifetime = applicationLifetime;
            _Logger = logger;

            _ApplicationLifetime.ApplicationStopped.Register(() =>
            {
                _Logger.LogInformation(new EventId(10001), "游戏虚拟世界服务正常下线。");
            });
        }

        readonly IServiceProvider _Services;

        IHostApplicationLifetime _ApplicationLifetime;
        ILogger<GameHostedService> _Logger;

        /// <summary>
        /// 使用的服务容器。
        /// </summary>
        public IServiceProvider Services { get => _Services; }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _Services.CreateScope();
            var service = scope.ServiceProvider;
            CreateDb(service);
            UpdateDatabase(service);
            var result = Task.Factory.StartNew(c =>
            {
                //Thread thread = new Thread(() => CreateNewUserAndChar())
                //{
                //    IsBackground = true,
                //    Priority = ThreadPriority.Lowest,
                //};
                //#if DEBUG
                //                thread.Start();
                //#else
                //                thread.Start();
                //#endif
                //                Task.Run(CreateGameManager);    //强制初始化所有服务以加速
                Task.Run(SetDbConfig);  //设置数据库配置项
                var logger = _Services.GetService<ILogger<GameHostedService>>();
                logger?.LogInformation("游戏虚拟世界服务成功上线。");
                using var scope = _Services.CreateScope();
                var service = scope.ServiceProvider;
                var mailManager = service.GetService<GameMailManager>();
                Task.Run(mailManager.ClearMail);
                Task.Run(InitializeAdmin);
            }, _Services, cancellationToken);

            Test();

            return result;
        }

        /// <summary>
        /// 配置数据库。
        /// </summary>
        private void SetDbConfig()
        {
            using var scope = _Services.CreateScope();
            var svc = scope.ServiceProvider;
            var sqlSb = AutoClearPool<StringBuilder>.Shared.Get();
            using var dwSb = DisposeHelper.Create(c => AutoClearPool<StringBuilder>.Shared.Return(c), sqlSb);
            #region 设置sql server使用内存，避免sql server 贪婪使用内存导致内存过大
            var sqlMemoryInMB = 4096;   //SQL服务器内存大小
            var mi = new MEMORYINFO(); mi.Initialize();
            if (Win32Methods.GlobalMemoryStatusEx(ref mi))
            {
                var tmp = (decimal)mi.ullTotalPhys / 1024 / 1024;
                sqlMemoryInMB = Math.Max(sqlMemoryInMB, (int)Math.Round(tmp / 3));
            }

            var fact = _Services.GetService<IDbContextFactory<GY02UserContext>>();
            using var db = fact.CreateDbContext();
            sqlSb.AppendLine(@$"EXEC sys.sp_configure N'show advanced options', N'1'  RECONFIGURE WITH OVERRIDE;" +
                $"EXEC sys.sp_configure N'max server memory (MB)', N'{sqlMemoryInMB}';" +
                "RECONFIGURE WITH OVERRIDE;" +
                "EXEC sys.sp_configure N'show advanced options', N'0'  RECONFIGURE WITH OVERRIDE;");
            #endregion

            //压缩
            var table = db?.Model.FindEntityType(typeof(VirtualThing))?.GetTableMappings();

            var tn = db?.Model.FindEntityType(typeof(VirtualThing))?.GetTableName();
            sqlSb.AppendLine($"ALTER TABLE {tn} REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = PAGE);");
            sqlSb.AppendLine($"ALTER INDEX IX_VirtualThings_ExtraGuid_ExtraDecimal_ExtraString ON {tn} REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = PAGE);");
            sqlSb.AppendLine($"ALTER INDEX IX_VirtualThings_ExtraGuid_ExtraString_ExtraDecimal ON {tn} REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = PAGE);");

            try
            {
                db?.Database.ExecuteSqlRaw(sqlSb.ToString());
            }
            catch (Exception err)
            {
                Trace.WriteLine(err.Message);
            }
#if !DEBUG  //若正式运行版本

#endif
        }

        /// <summary>
        /// 升级数据库内容。
        /// </summary>
        private void UpdateDatabase(IServiceProvider services)
        {
            var dbUser = services.GetRequiredService<GY02UserContext>();
            Dictionary<Guid, string> dic = new Dictionary<Guid, string>
            {
                {
                    Guid.Parse("{1FE2EF96-5C0C-46F7-8377-7EA9371871A6}"),
                        "UPDATE [dbo].[VirtualThings] SET [ExtraDateTime] = cast(JSON_Value([JsonObjectString],'$.CreateDateTime') as datetime2) where JSON_Value([JsonObjectString],'$.CreateDateTime') is not null;"+
                        "UPDATE [dbo].[VirtualThings] SET [JsonObjectString] = JSON_MODIFY([JsonObjectString],'$.CreateDateTime',null)"
                },
                {
                    Guid.Parse("{17B22D6F-AA1E-489A-BE50-9A30AA893045}"),
                        "UPDATE [dbo].[VirtualThings] SET [ExtraDecimal] =cast(JSON_Value([JsonObjectString],'$.Count') as decimal) where JSON_Value([JsonObjectString],'$.Count') is not null;"+
                        "UPDATE [dbo].[VirtualThings] SET [JsonObjectString] = JSON_MODIFY([JsonObjectString],'$.Count',null)"
                },
            };
            var names = dic.Keys.Select(c => c.ToString()).ToArray();
            var ids = dbUser.ServerConfig.Where(c => names.Contains(c.Name)).AsEnumerable().AsEnumerable().Where(c => Guid.TryParse(c.Name, out var id)).Select(c => Guid.Parse(c.Name)).ToHashSet();   //已经存在的内容升级

            dbUser.Database.SetCommandTimeout(TimeSpan.FromSeconds(60));
            foreach (var kvp in dic)    //执行升级语句
            {
                if (ids.Contains(kvp.Key)) continue;
                try
                {
                    dbUser.Database.ExecuteSqlRaw(kvp.Value);
                    var key = kvp.Key.ToString();
                    var config = new ServerConfigItem { Name = key, Value = kvp.Value };
                    dbUser.Add(config);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            dbUser.SaveChanges();
        }

        /// <summary>
        /// 初始化管理员账号。
        /// </summary>
        void InitializeAdmin()
        {
            using var dw = DisposeHelper.Create(SingletonLocker.TryEnter, SingletonLocker.Exit, ProjectContent.AdminLoginName, Timeout.InfiniteTimeSpan);
            if (dw.IsEmpty) throw new InvalidOperationException("无法锁定管理员登录名。");

            var store = _Services.GetRequiredService<GameAccountStoreManager>();
            var fac = _Services.GetRequiredService<IDbContextFactory<GY02UserContext>>();
            using var db = fac.CreateDbContext();
            var tmp = db.VirtualThings.FirstOrDefault(c => c.ExtraGuid == ProjectContent.UserTId && c.ExtraString == ProjectContent.AdminLoginName);
            if (tmp is null)    //若需要初始化管理员账号
            {
                using var scope = _Services.CreateScope();
                CreateAccountCommand createUser = new CreateAccountCommand { LoginName = ProjectContent.AdminLoginName, Pwd = ProjectContent.AdminPwd };
                var handler = scope.ServiceProvider.GetRequiredService<SyncCommandManager>();
                handler.Handle(createUser);
                if (createUser.HasError) throw new InvalidOperationException("无法创建超管账号。");
                using var dwUser = DisposeHelper.Create(store.Lock, store.Unlock, createUser.User.Key, Timeout.InfiniteTimeSpan);
                if (dw.IsEmpty) throw new InvalidOperationException("无法锁定管理员对象。");
                var gc = createUser.User.CurrentChar;
                gc.Roles.Add(ProjectContent.SupperAdminRole);
                store.Save(createUser.User.Key);
            }
        }

        /// <summary>
        /// 迁移升级数据库。
        /// </summary>
        /// <param name="services"></param>
        private void CreateDb(IServiceProvider services)
        {
            var logger = services.GetRequiredService<ILogger<GameHostedService>>();
            try
            {
                var loggingContext = services.GetRequiredService<GY02LogginContext>();
                GY02LogginMigrateDbInitializer.Initialize(loggingContext);
                logger.LogTrace($"日志数据库已正常升级。");

                var tContext = services.GetRequiredService<GY02TemplateContext>();
                TemplateMigrateDbInitializer.Initialize(tContext);
                logger.LogTrace($"模板数据库已正常升级。");

                var context = services.GetRequiredService<GY02UserContext>();
                MigrateDbInitializer.Initialize(context);
                logger.LogTrace("用户数据库已正常升级。");

                //var loggingDb = services.GetService<GameLoggingDbContext>();
                //if (loggingDb != null)
                //{
                //    GameLoggingMigrateDbInitializer.Initialize(loggingDb);
                //    logger.LogTrace("日志数据库已正常升级。");
                //}
            }
            catch (Exception err)
            {
                logger.LogError(err, "升级数据库出现错误。");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        [Conditional("DEBUG")]
        private void Test(string str = null)
        {
            var store = _Services.GetService<GameAccountStoreManager>();
            var mapper = _Services.GetService<IMapper>();
            var sw = Stopwatch.StartNew();
            var cache = _Services.GetService<IMemoryCache>();

            #region 测试用代码
            try
            {
                var svc = _Services.GetRequiredService<T127Manager>();
                var svc1 = _Services.GetRequiredService<LoginNameGenerator>();
                using HttpClient client = new HttpClient();
                //var r = svc.GetRefreshTokenFromCode(client, svc.Code, svc._ClientId, svc._ClientSecret);
                //var str1 = r.Content.ReadAsStringAsync().Result;
                //var b = svc.GetOrderState("com.duangphl.07", "hpohhpfgbielodmhiflcefhd.AO-J1OwdtIEgJGxcMDtxK880anOSCy7yirhq0W6S4--tlDmmTrZOEv-CLcJzMwBuxLJ1xiw_1uaTX2-i4dppDQAY0SPTAeFHEFl6V1yTpTwVxBKiObGjjlA",
                //    out var result);
                //var s1 = $"val={null}";
                //var svc1228 = _Services.GetRequiredService<T1228Manager>();
                //var str1 = "event=orderPayed&orderId=1&productType=inapp&productCode=2&originOrderId=3&originInfo=4&customInfo={\"productType\":\"inapp\",\"productId\":\"2\",\"roleInfo\":{\"roleId\":\"\",\"roleName\":\"\",\"roleLevel\":\"\",\"serverName\":\"\",\"vipLevel\":\"\"}}&secret=YDjCiVmvo8KJnGCwoKZ5EpyemwR6XWt8x0bR";
                //var str2 = svc1228.GetSign(str1);
                int i = 1023;
                var str1 = $"FY{Interlocked.Increment(ref i):X6}";
                //Cult();
            }
            #endregion 测试用代码
            catch (Exception)
            {
            }
            finally
            {
                sw.Stop();
                Debug.WriteLine($"测试用时:{sw.ElapsedMilliseconds:0.0}ms");
            }
        }

        [Conditional("DEBUG")]
        void Cult()
        {
            const string cultNames = "aa,aa-DJ,aa-ER,aa-ET,af,af-NA,af-ZA,agq,agq-CM,ak,ak-GH,sq,sq-AL,sq-MK,gsw,gsw-FR,gsw-LI,gsw-CH,am,am-ET,ar,ar-DZ,ar-BH,ar-TD,ar-KM,ar-DJ,ar-EG,ar-ER,ar-IQ,ar-IL,ar-JO,ar-KW,ar-LB,ar-LY,ar-MR,ar-MA,ar-OM,ar-PS,ar-QA,ar-SA,ar-SO,ar-SS,ar-SD,ar-SY,ar-TN,ar-AE,ar-001,ar-YE,hy,hy-AM,as,as-IN,ast,ast-ES,asa,asa-TZ,az-Cyrl,az-Cyrl-AZ,az,az-Latn,az-Latn-AZ,ksf,ksf-CM,bm,bm-Latn-ML,bn,bn-BD,bn-IN,bas,bas-CM,ba,ba-RU,eu,eu-ES,be,be-BY,bem,bem-ZM,bez,bez-TZ,byn,byn-ER,brx,brx-IN,bs-Cyrl,bs-Cyrl-BA,bs-Latn,bs,bs-Latn-BA,br,br-FR,bg,bg-BG,my,my-MM,ca,ca-AD,ca-FR,ca-IT,ca-ES,ceb,ceb-Latn,ceb-Latn-PH,tzm-Arab-MA,tzm-Latn-MA,ku,ku-Arab,ku-Arab-IQ,ccp,ccp-Cakm,ccp-Cakm-BD,ccp-Cakm-IN,ce-RU,chr,chr-Cher,chr-Cher-US,cgg,cgg-UG,zh-Hans,zh,zh-CN,zh-SG,zh-Hant,zh-HK,zh-MO,zh-TW,cu-RU,swc,swc-CD,kw,kw-GB,co,co-FR,hr,hr-HR,hr-BA,cs,cs-CZ,da,da-DK,da-GL,prs,prs-AF,dv,dv-MV,dua,dua-CM,nl,nl-AW,nl-BE,nl-BQ,nl-CW,nl-NL,nl-SX,nl-SR,dz,dz-BT,ebu,ebu-KE,en,en-AS,en-AI,en-AG,en-AU,en-AT,en-BS,en-BB,en-BE,en-BZ,en-BM,en-BW,en-IO,en-VG,en-BI,en-CM,en-CA,en-029,en-KY,en-CX,en-CC,en-CK,en-CY,en-DK,en-DM,en-ER,en-150,en-FK,en-FI,en-FJ,en-GM,en-DE,en-GH,en-GI,en-GD,en-GU,en-GG,en-GY,en-HK,en-IN,en-IE,en-IM,en-IL,en-JM,en-JE,en-KE,en-KI,en-LS,en-LR,en-MO,en-MG,en-MW,en-MY,en-MT,en-MH,en-MU,en-FM,en-MS,en-NA,en-NR,en-NL,en-NZ,en-NG,en-NU,en-NF,en-MP,en-PK,en-PW,en-PG,en-PN,en-PR,en-PH,en-RW,en-KN,en-LC,en-VC,en-WS,en-SC,en-SL,en-SG,en-SX,en-SI,en-SB,en-ZA,en-SS,en-SH,en-SD,en-SZ,en-SE,en-CH,en-TZ,en-TK,en-TO,en-TT,en-TC,en-TV,en-UG,en-AE,en-GB,en-US,en-UM,en-VI,en-VU,en-001,en-ZM,en-ZW,eo,eo-001,et,et-EE,ee,ee-GH,ee-TG,ewo,ewo-CM,fo,fo-DK,fo-FO,fil,fil-PH,fi,fi-FI,fr,fr-DZ,fr-BE,fr-BJ,fr-BF,fr-BI,fr-CM,fr-CA,fr-029,fr-CF,fr-TD,fr-KM,fr-CG,fr-CD,fr-CI,fr-DJ,fr-GQ,fr-FR,fr-GF,fr-PF,fr-GA,fr-GP,fr-GN,fr-HT,fr-LU,fr-MG,fr-ML,fr-MQ,fr-MR,fr-MU,fr-YT,fr-MA,fr-NC,fr-NE,fr-MC,fr-RE,fr-RW,fr-BL,fr-MF,fr-PM,fr-SN,fr-SC,fr-CH,fr-SY,fr-TG,fr-TN,fr-VU,fr-WF,fy,fy-NL,fur,fur-IT,ff,ff-Latn,ff-Latn-BF,ff-CM,ff-Latn-CM,ff-Latn-GM,ff-Latn-GH,ff-GN,ff-Latn-GN,ff-Latn-GW,ff-Latn-LR,ff-MR,ff-Latn-MR,ff-Latn-NE,ff-NG,ff-Latn-NG,ff-Latn-SN,ff-Latn-SL,gl,gl-ES,lg,lg-UG,ka,ka-GE,de,de-AT,de-BE,de-DE,de-IT,de-LI,de-LU,de-CH,el,el-CY,el-GR,kl,kl-GL,gn,gn-PY,gu,gu-IN,guz,guz-KE,ha,ha-Latn,ha-Latn-GH,ha-Latn-NE,ha-Latn-NG,haw,haw-US,he,he-IL,hi,hi-IN,hu,hu-HU,is,is-IS,ig,ig-NG,id,id-ID,ia,ia-FR,ia-001,iu,iu-Latn,iu-Latn-CA,iu-Cans,iu-Cans-CA,ga,ga-IE,it,it-IT,it-SM,it-CH,it-VA,ja,ja-JP,jv,jv-Latn,jv-Latn-ID,dyo,dyo-SN,kea,kea-CV,kab,kab-DZ,kkj,kkj-CM,kln,kln-KE,kam,kam-KE,kn,kn-IN,kr-Latn-NG,ks,ks-Arab,ks-Arab-IN,ks-Deva-IN,kk,kk-KZ,km,km-KH,quc,quc-Latn-GT,ki,ki-KE,rw,rw-RW,sw,sw-KE,sw-TZ,sw-UG,kok,kok-IN,ko,ko-KR,ko-KP,khq,khq-ML,ses,ses-ML,nmg,nmg-CM,ky,ky-KG,ku-Arab-IR,lkt,lkt-US,lag,lag-TZ,lo,lo-LA,la-VA,lv,lv-LV,ln,ln-AO,ln-CF,ln-CG,ln-CD,lt,lt-LT,nds,nds-DE,nds-NL,dsb,dsb-DE,lu,lu-CD,luo,luo-KE,lb,lb-LU,luy,luy-KE,mk,mk-MK,jmc,jmc-TZ,mgh,mgh-MZ,kde,kde-TZ,mg,mg-MG,ms,ms-BN,ms-MY,ml,ml-IN,mt,mt-MT,gv,gv-IM,mi,mi-NZ,arn,arn-CL,mr,mr-IN,mas,mas-KE,mas-TZ,mzn-IR,mer,mer-KE,mgo,mgo-CM,moh,moh-CA,mn,mn-Cyrl,mn-MN,mn-Mong,mn-Mong-CN,mn-Mong-MN,mfe,mfe-MU,mua,mua-CM,nqo,nqo-GN,naq,naq-NA,ne,ne-IN,ne-NP,nnh,nnh-CM,jgo,jgo-CM,lrc-IQ,lrc-IR,nd,nd-ZW,no,nb,nb-NO,nn,nn-NO,nb-SJ,nus,nus-SD,nus-SS,nyn,nyn-UG,oc,oc-FR,or,or-IN,om,om-ET,om-KE,os,os-GE,os-RU,ps,ps-AF,ps-PK,fa,fa-AF,fa-IR,pl,pl-PL,pt,pt-AO,pt-BR,pt-CV,pt-GQ,pt-GW,pt-LU,pt-MO,pt-MZ,pt-PT,pt-ST,pt-CH,pt-TL,prg-001,qps-ploca,qps-ploc,qps-plocm,pa,pa-Arab,pa-IN,pa-Arab-PK,quz,quz-BO,quz-EC,quz-PE,ksh,ksh-DE,ro,ro-MD,ro-RO,rm,rm-CH,rof,rof-TZ,rn,rn-BI,ru,ru-BY,ru-KZ,ru-KG,ru-MD,ru-RU,ru-UA,rwk,rwk-TZ,ssy,ssy-ER,sah,sah-RU,saq,saq-KE,smn,smn-FI,smj,smj-NO,smj-SE,se,se-FI,se-NO,se-SE,sms,sms-FI,sma,sma-NO,sma-SE,sg,sg-CF,sbp,sbp-TZ,sa,sa-IN,gd,gd-GB,seh,seh-MZ,sr-Cyrl,sr-Cyrl-BA,sr-Cyrl-ME,sr-Cyrl-RS,sr-Cyrl-CS,sr-Latn,sr,sr-Latn-BA,sr-Latn-ME,sr-Latn-RS,sr-Latn-CS,nso,nso-ZA,tn,tn-BW,tn-ZA,ksb,ksb-TZ,sn,sn-Latn,sn-Latn-ZW,sd,sd-Arab,sd-Arab-PK,si,si-LK,sk,sk-SK,sl,sl-SI,xog,xog-UG,so,so-DJ,so-ET,so-KE,so-SO,st,st-ZA,nr,nr-ZA,st-LS,es,es-AR,es-BZ,es-VE,es-BO,es-BR,es-CL,es-CO,es-CR,es-CU,es-DO,es-EC,es-SV,es-GQ,es-GT,es-HN,es-419,es-MX,es-NI,es-PA,es-PY,es-PE,es-PH,es-PR,es-ES_tradnl,es-ES,es-US,es-UY,zgh,zgh-Tfng-MA,zgh-Tfng,ss,ss-ZA,ss-SZ,sv,sv-AX,sv-FI,sv-SE,syr,syr-SY,shi,shi-Tfng,shi-Tfng-MA,shi-Latn,shi-Latn-MA,dav,dav-KE,tg,tg-Cyrl,tg-Cyrl-TJ,tzm,tzm-Latn,tzm-Latn-DZ,ta,ta-IN,ta-MY,ta-SG,ta-LK,twq,twq-NE,tt,tt-RU,te,te-IN,teo,teo-KE,teo-UG,th,th-TH,bo,bo-IN,bo-CN,tig,tig-ER,ti,ti-ER,ti-ET,to,to-TO,ts,ts-ZA,tr,tr-CY,tr-TR,tk,tk-TM,uk,uk-UA,hsb,hsb-DE,ur,ur-IN,ur-PK,ug,ug-CN,uz-Arab,uz-Arab-AF,uz-Cyrl,uz-Cyrl-UZ,uz,uz-Latn,uz-Latn-UZ,vai,vai-Vaii,vai-Vaii-LR,vai-Latn-LR,vai-Latn,ca-ES-valencia,ve,ve-ZA,vi,vi-VN,vo,vo-001,vun,vun-TZ,wae,wae-CH,cy,cy-GB,wal,wal-ET,wo,wo-SN,xh,xh-ZA,yav,yav-CM,ii,ii-CN,yi-001,yo,yo-BJ,yo-NG,dje,dje-NE,zu,zu-ZA,";
            var names = cultNames.Split(',');
            var sb = new StringBuilder();
            sb.AppendLine($"{nameof(CultureInfo.DisplayName)}\t{nameof(CultureInfo.Name)}\t{nameof(CultureInfo.EnglishName)}\t{nameof(CultureInfo.LCID)}");
            foreach (var name in names)
            {
                var tmp = new CultureInfo(name);
                sb.AppendLine($"{tmp.DisplayName}\t{tmp.Name}\t{tmp.EnglishName}\t{tmp.LCID}");
            }
            using var file = File.OpenWrite("C:\\st.txt");
            using var sw = new StreamWriter(file);
            sw.Write(sb.ToString());
        }
    }

    //private unsafe void Awake()
    //{
    //    byte[] sendByte = Encoding.ASCII.GetBytes("");//FORWARDERS

    //    fixed (byte* pointerToFirst = &sendByte[0])
    //    {
    //    }
    //}
}