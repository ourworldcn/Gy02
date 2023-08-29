using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OW.SyncCommand
{
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class SyncCommandManager : OwDisposableBase
    {
        public SyncCommandManager()
        {

        }

        public SyncCommandManager(IServiceProvider service)
        {
            _Service = service;

        }

        IServiceProvider _Service;

        private Dictionary<string, object> _Items;
        /// <summary>
        /// 当前范围内的一些数据。
        /// </summary>
        public IDictionary<string, object> Items => _Items ??= AutoClearPool<Dictionary<string, object>>.Shared.Get();

        [DebuggerHidden]
        public void Handle<T>(T command) where T : ISyncCommand
        {
            #region 预处理

            #endregion 预处理
            _OrderNumber = 0;
            var pre = _Service.GetServices<ISyncCommandHandling<T>>();
            var coll = _Service.GetServices<ISyncCommandHandler<T>>();
            Exception exception = null;
            var post = _Service.GetServices<ISyncCommandHandled<T>>();

            try
            {
                pre.SafeForEach(c =>
                {
                    c.Handling(command);
                });
            }
            catch (Exception)
            {
                //TODO 暂时忽略命令预处理的异常
            }
            try
            {
                coll.SafeForEach(c =>
                {
                    c.Handle(command);
                    _OrderNumber++;
                });
            }
            catch (Exception excp)
            {
                exception = excp;
                throw;
            }
            finally
            {
                try
                {
                    post.SafeForEach(c =>
                    {
                        c.Handled(command);
                    });
                }
                catch
                {
                    //TODO 暂时忽略命令后处理的异常
                }
            }
        }

        private int _OrderNumber;

        public int OrderNumber { get => _OrderNumber; set => _OrderNumber = value; }

        #region IDisposable接口相关

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                if (_Items != null)
                {
                    AutoClearPool<Dictionary<string, object>>.Shared.Return(_Items);
                    _Items = null;
                }
                base.Dispose(disposing);
            }
        }

        #endregion IDisposable接口相关
    }

    public static class SyncCommandManagerExtensions
    {
        public static IServiceCollection UseSyncCommand(this IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            var coll = from tmp in assemblies.SelectMany(c => c.GetTypes())
                       let i = tmp.FindInterfaces((c1, c2) => c1.IsGenericType && c1.GetGenericTypeDefinition() == typeof(ISyncCommandHandler<>), null).FirstOrDefault()
                       where i != null && tmp.IsClass && !tmp.IsAbstract
                       select (Type: tmp, @interface: i);
            foreach (var item in coll)
            {
                services.AddScoped(item.@interface, item.Type);
            }
            return services;
        }
    }

    /// <summary>
    /// 在同一个线程(net)中处理的命令对象的专用标记接口。
    /// </summary>
    public interface ISyncCommand
    {

    }

    /// <summary>
    /// 有通用返回值的命令。
    /// </summary>
    public interface IResultCommand : ISyncCommand
    {
        /// <summary>
        /// 错误码，参见 ErrorCodes。
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// 是否有错误。不设置则使用<see cref="ErrorCode"/>来判定。
        /// </summary>
        /// <value>0没有错误，其它数值含义由应用定义。</value>
        public bool HasError { get; set; }

        /// <summary>
        /// 调试信息，如果发生错误，这里给出简要说明。
        /// </summary>
        public string DebugMessage { get; set; }
    }

    /// <summary>
    /// 游戏命令处理器的基础接口。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISyncCommandHandler<T> where T : ISyncCommand
    {
        /// <summary>
        /// 命令处理函数。
        /// </summary>
        /// <param name="command"></param>
        public abstract void Handle(T command);
    }

    /// <summary>
    /// 对命令预处理的接口。多个同命令的预处理接口被调用的顺序无法确定。
    /// </summary>
    /// <typeparam name="TCommand"></typeparam>
    public interface ISyncCommandHandling<TCommand> where TCommand : ISyncCommand
    {
        public void Handling(TCommand command);
    }

    /// <summary>
    /// 对命令进行后处理的接口。多个同命令的后处理接口被调用的顺序无法确定。
    /// </summary>
    /// <typeparam name="TCommand"></typeparam>
    public interface ISyncCommandHandled<TCommand> where TCommand : ISyncCommand
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="exception">若命令的处理过程中(<see cref="ISyncCommandHandler{T}"/>)引发了异常则在此给出，否则为空引用。</param>
        public void Handled(TCommand command, Exception exception = null);

    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class SyncCommandBase : IResultCommand, ISyncCommand
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public SyncCommandBase()
        {

        }

        #region IResultWorkData 接口相关

        private bool? _HasError;

        /// <summary>
        /// 是否有错误。不设置则使用<see cref="ErrorCode"/>来判定。
        /// </summary>
        /// <value>0没有错误，其它数值含义由应用定义。</value>
        public bool HasError { get => _HasError ??= ErrorCode != 0; set => _HasError = value; }

        /// <summary>
        /// 错误码，参见 ErrorCodes。
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// 调试用的提示性信息。
        /// </summary>
        private string _ErrorMessage;

        /// <summary>
        /// 调试信息，如果发生错误，这里给出简要说明。
        /// </summary>
        public string DebugMessage
        {
            get => _ErrorMessage ??= new Win32Exception(ErrorCode).Message;
            set => _ErrorMessage = value;
        }

        #endregion IResultWorkData 接口相关
    }

    /// <summary>
    /// 命令处理类的基类，可以在构造函数中注入必须的对象。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SyncCommandHandlerBase<T> : ISyncCommandHandler<T> where T : ISyncCommand
    {
        protected SyncCommandHandlerBase()
        {

        }



        public abstract void Handle(T command);
    }

    /// <summary>
    /// 
    /// </summary>
    public static class SyncCommandBaseExtensions
    {
        /// <summary>
        /// 从<see cref="VWorld"/>对象获取错误信息。
        /// </summary>
        /// <param name="obj"></param>
        public static void FillErrorFromWorld(this IResultCommand obj)
        {
            obj.ErrorCode = OwHelper.GetLastError();
            obj.DebugMessage = OwHelper.GetLastErrorMessage();
            obj.HasError = 0 != obj.ErrorCode;
        }

        /// <summary>
        /// 从另一个对象填充错误。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="src"></param>
        public static void FillErrorFrom(this IResultCommand obj, IResultCommand src)
        {
            obj.ErrorCode = src.ErrorCode;
            obj.DebugMessage = src.DebugMessage;
            obj.HasError = src.HasError;
        }
    }
}
