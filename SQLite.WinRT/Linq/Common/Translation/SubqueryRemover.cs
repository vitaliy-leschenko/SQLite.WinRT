﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using SQLite.WinRT.Linq.Common.Expressions;

namespace SQLite.WinRT.Linq.Common.Translation
{
    /// <summary>
    ///     Removes one or more SelectExpression's by rewriting the expression tree to not include them, promoting
    ///     their from clause expressions and rewriting any column expressions that may have referenced them to now
    ///     reference the underlying data directly.
    /// </summary>
    public class SubqueryRemover : DbExpressionVisitor
    {
        private readonly Dictionary<TableAlias, Dictionary<string, Expression>> map;
        private readonly HashSet<SelectExpression> selectsToRemove;

        private SubqueryRemover(IEnumerable<SelectExpression> selectsToRemove)
        {
            this.selectsToRemove = new HashSet<SelectExpression>(selectsToRemove);
            map = this.selectsToRemove.ToDictionary(
                d => d.Alias, d => d.Columns.ToDictionary(d2 => d2.Name, d2 => d2.Expression));
        }

        public static SelectExpression Remove(SelectExpression outerSelect, params SelectExpression[] selectsToRemove)
        {
            return Remove(outerSelect, (IEnumerable<SelectExpression>) selectsToRemove);
        }

        public static SelectExpression Remove(SelectExpression outerSelect,
            IEnumerable<SelectExpression> selectsToRemove)
        {
            return (SelectExpression) new SubqueryRemover(selectsToRemove).Visit(outerSelect);
        }

        public static ProjectionExpression Remove(ProjectionExpression projection,
            params SelectExpression[] selectsToRemove)
        {
            return Remove(projection, (IEnumerable<SelectExpression>) selectsToRemove);
        }

        public static ProjectionExpression Remove(
            ProjectionExpression projection, IEnumerable<SelectExpression> selectsToRemove)
        {
            return (ProjectionExpression) new SubqueryRemover(selectsToRemove).Visit(projection);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            if (selectsToRemove.Contains(select))
            {
                return Visit(select.From);
            }
            return base.VisitSelect(@select);
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            Dictionary<string, Expression> nameMap;
            if (map.TryGetValue(column.Alias, out nameMap))
            {
                Expression expr;
                if (nameMap.TryGetValue(column.Name, out expr))
                {
                    return Visit(expr);
                }
                throw new Exception("Reference to undefined column");
            }
            return column;
        }
    }
}