using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace OW.Game.Conditional
{
    public class GameConditional : Collection<GameConditionalItem>
    {
        public GameConditional()
        {

        }

    }

    public class GameConditionalItem
    {
        [JsonPropertyName("op")]
        public string Operator
        {
            get; set;
        }

        [JsonPropertyName("lo")]
        public GameOperand LeftOperand
        {
            get; set;
        }

        [JsonPropertyName("ro")]
        public GameOperand RightOperand
        {
            get; set;
        }
    }

    public class GameOperand
    {

        public string RefType
        {
            get;
            set;
        }

        public string Value
        {
            get;
            set;
        }
    }
}