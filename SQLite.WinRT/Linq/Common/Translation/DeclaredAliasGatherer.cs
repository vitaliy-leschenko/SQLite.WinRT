// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Collections.Generic;
using System.Linq.Expressions;
using SQLite.WinRT.Linq.Common.Expressions;

namespace SQLite.WinRT.Linq.Common.Translation
{
    /// <summary>
    ///     returns the set of all aliases produced by a query source
    /// </summary>
    public class DeclaredAliasGatherer : DbExpressionVisitor
    {
        private readonly HashSet<TableAlias> aliases;

        private DeclaredAliasGatherer()
        {
            aliases = new HashSet<TableAlias>();
        }

        public static HashSet<TableAlias> Gather(Expression source)
        {
            var gatherer = new DeclaredAliasGatherer();
            gatherer.Visit(source);
            return gatherer.aliases;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            aliases.Add(select.Alias);
            return select;
        }

        protected override Expression VisitTable(TableExpression table)
        {
            aliases.Add(table.Alias);
            return table;
        }
    }
}