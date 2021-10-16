using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Denbot.Common.Attributes;
using Denbot.Common.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Denbot.Common.Repositories {
    public class MongoRepository<TDocument> : IMongoRepository<TDocument> where TDocument : IDocument {
        private readonly IMongoCollection<TDocument> _collection;

        public MongoRepository(IMongoDatabase database) {
            _collection = database.GetCollection<TDocument>(GetCollectionName(typeof(TDocument)));
        }

        public IQueryable<TDocument> AsQueryable() {
            return _collection.AsQueryable();
        }

        public IEnumerable<TDocument> FilterBy(Expression<Func<TDocument, bool>> filterExpression) {
            return _collection.Find(filterExpression).ToEnumerable();
        }

        public IEnumerable<TProjected> FilterBy<TProjected>(Expression<Func<TDocument, bool>> filterExpression, 
            Expression<Func<TDocument, TProjected>> projectionExpression) {
            return _collection.Find(filterExpression).Project(projectionExpression).ToEnumerable();
        }

        public async Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression) {
            return await _collection.Find(filterExpression).FirstOrDefaultAsync();
        }

        public async Task<TDocument> FindByIdAsync(string id) {
            var objectId = new ObjectId(id);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            return await _collection.Find(filter).SingleOrDefaultAsync();
        }

        public async Task InsertOneAsync(TDocument document) {
           await _collection.InsertOneAsync(document);
        }

        public async Task InsertManyAsync(IEnumerable<TDocument> documents) {
            await _collection.InsertManyAsync(documents);
        }

        public async Task ReplaceOneAsync(TDocument document) {
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, document.Id);
            await _collection.FindOneAndReplaceAsync(filter, document);
        }

        public async Task DeleteOneAsync(Expression<Func<TDocument, bool>> filterExpression) {
            await _collection.FindOneAndDeleteAsync(filterExpression);
        }

        public async Task DeleteByIdAsync(string id) {
            var objectId = new ObjectId(id);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            await _collection.FindOneAndDeleteAsync(filter);
        }

        public async Task DeleteManyAsync(Expression<Func<TDocument, bool>> filterExpression) {
            await _collection.DeleteManyAsync(filterExpression);
        }

        private static string GetCollectionName(ICustomAttributeProvider documentType) {
            return ((BsonCollectionAttribute) documentType.GetCustomAttributes(typeof(BsonCollectionAttribute), true)
                .FirstOrDefault())?.CollectionName;
        }
    }
}