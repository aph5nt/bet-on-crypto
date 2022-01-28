using System;
using Newtonsoft.Json.Serialization;

namespace Web.Infrastructure
{
    public class SignalRContractResolver : IContractResolver
    {
        private readonly IContractResolver _defaultContractSerializer;

        public SignalRContractResolver()
        {
            _defaultContractSerializer = new DefaultContractResolver();
        }

        public JsonContract ResolveContract(Type type)
        {
            return _defaultContractSerializer.ResolveContract(type);
        }
    }
}