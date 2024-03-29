﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Denbot.Common.Entities;

namespace Denbot.Common.Repositories {
    public interface IMongoRepository<TDocument> where TDocument : IDocument {
        IQueryable<TDocument> AsQueryable();
        IEnumerable<TDocument> FilterBy(Expression<Func<TDocument, bool>> filterExpression);
        IEnumerable<TProjected> FilterBy<TProjected>(Expression<Func<TDocument, bool>> filterExpression,
            Expression<Func<TDocument, TProjected>> projectionExpression);
        Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression);
        Task<TDocument> FindByIdAsync(string id);
        Task InsertOneAsync(TDocument document);
        Task InsertManyAsync(IEnumerable<TDocument> documents);
        Task ReplaceOneAsync(TDocument document);
        Task DeleteOneAsync(Expression<Func<TDocument, bool>> filterExpression);
        Task DeleteByIdAsync(string id);
        Task DeleteManyAsync(Expression<Func<TDocument, bool>> filterExpression);
    }
}