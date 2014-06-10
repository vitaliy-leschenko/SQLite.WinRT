// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq.Expressions;
using SQLite.WinRT.Linq.Common.Expressions;

namespace SQLite.WinRT.Linq.Common.Translation
{
    /// <summary>
    ///     Duplicate the query expression by making a copy with new table aliases
    /// </summary>
    public class QueryDuplicator : DbExpressionVisitor
    {
        private readonly Dictionary<TableAlias, TableAlias> map = new Dictionary<TableAlias, TableAlias>();

        public static Expression Duplicate(Expression expression)
        {
            return new QueryDuplicator().Visit(expression);
        }

        protected override Expression VisitTable(TableExpression table)
        {
            var newAlias = new TableAlias();
            map[table.Alias] = newAlias;
            return new TableExpression(newAlias, table.Entity, table.Name);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            var newAlias = new TableAlias();
            map[select.Alias] = newAlias;
            select = (SelectExpression) base.VisitSelect(select);
            return new SelectExpression(
                newAlias,
                select.Columns,
                select.From,
                select.Where,
                select.OrderBy,
                select.GroupBy,
                select.IsDistinct,
                select.Skip,
                select.Take,
                select.IsReverse);
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            TableAlias newAlias;
            if (map.TryGetValue(column.Alias, out newAlias))
            {
                return new ColumnExpression(column.Type, column.QueryType, newAlias, column.Name);
            }
            return column;
        }
    }
}