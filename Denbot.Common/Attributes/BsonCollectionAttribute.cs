using System;

namespace Denbot.Common.Attributes {
    [AttributeUsage(AttributeTargets.Class)]
    public class BsonCollectionAttribute : Attribute {
        public string CollectionName { get; }

        public BsonCollectionAttribute(string collectionName) {
            CollectionName = collectionName;
        }
    }
}