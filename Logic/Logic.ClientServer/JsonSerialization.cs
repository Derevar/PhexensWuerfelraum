﻿using Newtonsoft.Json;
using SimpleSockets.Messaging.MessageContracts;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace PhexensWuerfelraum.Logic.ClientServer
{
    public class JsonSerialization : IObjectSerializer
    {
        public object DeserializeBytesToObject(byte[] bytes, Type objType)
        {
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(bytes), objType);
        }

        public byte[] SerializeObjectToBytes(object anySerializableObject)
        {
            string jsonObject = Regex.Unescape(JsonConvert.SerializeObject(anySerializableObject, Formatting.Indented));
            return Encoding.UTF8.GetBytes(jsonObject);
        }
    }
}