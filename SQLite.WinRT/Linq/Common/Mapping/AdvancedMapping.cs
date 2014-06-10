// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.WinRT.Linq.Base;
using SQLite.WinRT.Linq.Common.Expressions;
using SQLite.WinRT.Linq.Common.Language;
using SQLite.WinRT.Linq.Common.Translation;

namespace SQLite.WinRT.Linq.Common.Mapping
{
    public abstract class MappingTable
    {
    }

    public abstract class AdvancedMapping : BasicMapping
    {
        public abstract bool IsNestedEntity(MappingEntity entity, MemberInfo member);

        public abstract IList<MappingTable> GetTables(MappingEntity entity);

        public abstract string GetAlias(MappingTable table);

        public abstract string GetAlias(MappingEntity entity, MemberInfo member);

        public abstract string GetTableName(MappingTable table);

        public abstract bool IsExtensionTable(MappingTable table);

        public abstract string GetExtensionRelatedAlias(MappingTable table);

        public abstract IEnumerable<string> GetExtensionKeyColumnNames(MappingTable table);

        public abstract IEnumerable<MemberInfo> GetExtensionRelatedMembers(MappingTable table);

        public override bool IsRelationship(MappingEntity entity, MemberInfo member)
        {
            return base.IsRelationship(entity, member) || IsNestedEntity(entity, member);
        }

        public override object CloneEntity(MappingEntity entity, object instance)
        {
            object clone = base.CloneEntity(entity, instance);

            // need to clone nested entities too
            foreach (MemberInfo mi in GetMappedMembers(entity))
            {
                if (IsNestedEntity(entity, mi))
                {
                    MappingEntity nested = GetRelatedEntity(entity, mi);
                    object nestedValue = mi.GetValue(instance);
                    if (nestedValue != null)
                    {
                        object nestedClone = CloneEntity(nested, mi.GetValue(instance));
                        mi.SetValue(clone, nestedClone);
                    }
                }
            }

            return clone;
        }

        public override bool IsModified(MappingEntity entity, object instance, object original)
        {
            if (base.IsModified(entity, instance, original))
            {
                return true;
            }

            // need to check nested entities too
            foreach (MemberInfo mi in GetMappedMembers(entity))
            {
                if (IsNestedEntity(entity, mi))
                {
                    MappingEntity nested = GetRelatedEntity(entity, mi);
                    if (IsModified(nested, mi.GetValue(instance), mi.GetValue(original)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override QueryMapper CreateMapper(QueryTranslator translator)
        {
            return new AdvancedMapper(this, translator);
        }
    }

    public class AdvancedMapper : BasicMapper
    {
        private readonly AdvancedMapping mapping;

        public AdvancedMapper(AdvancedMapping mapping, QueryTranslator translator)
            : base(mapping, translator)
        {
            this.mapping = mapping;
        }

        public virtual IEnumerable<MappingTable> GetDependencyOrderedTables(MappingEntity entity)
        {
            ILookup<string, MappingTable> lookup = mapping.GetTables(entity).ToLookup(t => mapping.GetAlias(t));
            return
                mapping.GetTables(entity).Sort(
                    t => mapping.IsExtensionTable(t) ? lookup[mapping.GetExtensionRelatedAlias(t)] : null);
        }

        public override EntityExpression GetEntityExpression(Expression root, MappingEntity entity)
        {
            // must be some complex type constructed from multiple columns
            var assignments = new List<EntityAssignment>();
            foreach (MemberInfo mi in mapping.GetMappedMembers(entity))
            {
                if (!mapping.IsAssociationRelationship(entity, mi))
                {
                    Expression me;
                    if (mapping.IsNestedEntity(entity, mi))
                    {
                        me = GetEntityExpression(root, mapping.GetRelatedEntity(entity, mi));
                    }
                    else
                    {
                        me = GetMemberExpression(root, entity, mi);
                    }
                    if (me != null)
                    {
                        assignments.Add(new EntityAssignment(mi, me));
                    }
                }
            }

            return new EntityExpression(entity, BuildEntityExpression(entity, assignments));
        }

        public override Expression GetMemberExpression(Expression root, MappingEntity entity, MemberInfo member)
        {
            if (mapping.IsNestedEntity(entity, member))
            {
                MappingEntity subEntity = mapping.GetRelatedEntity(entity, member);
                return GetEntityExpression(root, subEntity);
            }
            return base.GetMemberExpression(root, entity, member);
        }

        public override ProjectionExpression GetQueryExpression(MappingEntity entity)
        {
            IList<MappingTable> tables = mapping.GetTables(entity);
            if (tables.Count <= 1)
            {
                return base.GetQueryExpression(entity);
            }

            var aliases = new Dictionary<string, TableAlias>();
            MappingTable rootTable = tables.Single(ta => !mapping.IsExtensionTable(ta));
            var tex = new TableExpression(new TableAlias(), entity, mapping.GetTableName(rootTable));
            aliases.Add(mapping.GetAlias(rootTable), tex.Alias);
            Expression source = tex;

            foreach (MappingTable table in tables.Where(t => mapping.IsExtensionTable(t)))
            {
                var joinedTableAlias = new TableAlias();
                string extensionAlias = mapping.GetAlias(table);
                aliases.Add(extensionAlias, joinedTableAlias);

                List<string> keyColumns = mapping.GetExtensionKeyColumnNames(table).ToList();
                List<MemberInfo> relatedMembers = mapping.GetExtensionRelatedMembers(table).ToList();
                string relatedAlias = mapping.GetExtensionRelatedAlias(table);

                TableAlias relatedTableAlias;
                aliases.TryGetValue(relatedAlias, out relatedTableAlias);

                var joinedTex = new TableExpression(joinedTableAlias, entity, mapping.GetTableName(table));

                Expression cond = null;
                for (int i = 0, n = keyColumns.Count; i < n; i++)
                {
                    Type memberType = TypeHelper.GetMemberType(relatedMembers[i]);
                    DbQueryType colType = GetColumnType(entity, relatedMembers[i]);
                    var relatedColumn = new ColumnExpression(
                        memberType, colType, relatedTableAlias, mapping.GetColumnName(entity, relatedMembers[i]));
                    var joinedColumn = new ColumnExpression(memberType, colType, joinedTableAlias, keyColumns[i]);
                    Expression eq = joinedColumn.Equal(relatedColumn);
                    cond = (cond != null) ? cond.And(eq) : eq;
                }

                source = new JoinExpression(JoinType.SingletonLeftOuter, source, joinedTex, cond);
            }

            var columns = new List<ColumnDeclaration>();
            GetColumns(entity, aliases, columns);
            var root = new SelectExpression(new TableAlias(), columns, source, null);
            TableAlias[] existingAliases = aliases.Values.ToArray();

            Expression projector = GetEntityExpression(root, entity);
            var selectAlias = new TableAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projector, null, selectAlias, root.Alias);
            var proj = new ProjectionExpression(new SelectExpression(selectAlias, pc.Columns, root, null), pc.Projector);

            throw new NotImplementedException();
            //return (ProjectionExpression)this.Translator.Police.ApplyPolicy(proj, entity.ElementType);
        }

        private void GetColumns(MappingEntity entity, Dictionary<string, TableAlias> aliases,
            List<ColumnDeclaration> columns)
        {
            foreach (MemberInfo mi in mapping.GetMappedMembers(entity))
            {
                if (!mapping.IsAssociationRelationship(entity, mi))
                {
                    if (mapping.IsNestedEntity(entity, mi))
                    {
                        GetColumns(mapping.GetRelatedEntity(entity, mi), aliases, columns);
                    }
                    else if (mapping.IsColumn(entity, mi))
                    {
                        string name = mapping.GetColumnName(entity, mi);
                        string aliasName = mapping.GetAlias(entity, mi);
                        TableAlias alias;
                        aliases.TryGetValue(aliasName, out alias);
                        DbQueryType colType = GetColumnType(entity, mi);
                        var ce = new ColumnExpression(TypeHelper.GetMemberType(mi), colType, alias, name);
                        var cd = new ColumnDeclaration(name, ce, colType);
                        columns.Add(cd);
                    }
                }
            }
        }

        public override Expression GetInsertExpression(MappingEntity entity, Expression instance,
            LambdaExpression selector)
        {
            IList<MappingTable> tables = mapping.GetTables(entity);
            if (tables.Count < 2)
            {
                return base.GetInsertExpression(entity, instance, selector);
            }

            var commands = new List<Expression>();

            Dictionary<string, List<MemberInfo>> map = GetDependentGeneratedColumns(entity);
            var vexMap = new Dictionary<MemberInfo, Expression>();

            foreach (MappingTable table in GetDependencyOrderedTables(entity))
            {
                var tableAlias = new TableAlias();
                var tex = new TableExpression(tableAlias, entity, mapping.GetTableName(table));
                IEnumerable<ColumnAssignment> assignments = GetColumnAssignments(
                    tex,
                    instance,
                    entity,
                    (e, m) => mapping.GetAlias(e, m) == mapping.GetAlias(table) && !mapping.IsGenerated(e, m),
                    vexMap);
                IEnumerable<ColumnAssignment> totalAssignments =
                    assignments.Concat(GetRelatedColumnAssignments(tex, entity, table, vexMap));
                commands.Add(new InsertCommand(tex, totalAssignments));

                List<MemberInfo> members;
                if (map.TryGetValue(mapping.GetAlias(table), out members))
                {
                    CommandExpression d = GetDependentGeneratedVariableDeclaration(entity, table, members, instance,
                        vexMap);
                    commands.Add(d);
                }
            }

            if (selector != null)
            {
                commands.Add(GetInsertResult(entity, instance, selector, vexMap));
            }

            return new BlockCommand(commands);
        }

        private Dictionary<string, List<MemberInfo>> GetDependentGeneratedColumns(MappingEntity entity)
        {
            return (from xt in mapping.GetTables(entity).Where(t => mapping.IsExtensionTable(t))
                group xt by mapping.GetExtensionRelatedAlias(xt)).ToDictionary(
                    g => g.Key, g => g.SelectMany(xt => mapping.GetExtensionRelatedMembers(xt)).Distinct().ToList());
        }

        // make a variable declaration / initialization for dependent generated values
        private CommandExpression GetDependentGeneratedVariableDeclaration(
            MappingEntity entity,
            MappingTable table,
            List<MemberInfo> members,
            Expression instance,
            Dictionary<MemberInfo, Expression> map)
        {
            // first make command that retrieves the generated ids if any
            DeclarationCommand genIdCommand = null;
            List<MemberInfo> generatedIds =
                mapping.GetMappedMembers(entity).Where(
                    m => mapping.IsPrimaryKey(entity, m) && mapping.IsGenerated(entity, m)).ToList();
            if (generatedIds.Count > 0)
            {
                genIdCommand = GetGeneratedIdCommand(entity, members, map);

                // if that's all there is then just return the generated ids
                if (members.Count == generatedIds.Count)
                {
                    return genIdCommand;
                }
            }

            // next make command that retrieves the generated members
            // only consider members that were not generated ids
            members = members.Except(generatedIds).ToList();

            var tableAlias = new TableAlias();
            var tex = new TableExpression(tableAlias, entity, mapping.GetTableName(table));

            Expression where = null;
            if (generatedIds.Count > 0)
            {
                where =
                    generatedIds.Select((m, i) => GetMemberExpression(tex, entity, m).Equal(map[m]))
                        .Aggregate((x, y) => x.And(y));
            }
            else
            {
                where = GetIdentityCheck(tex, entity, instance);
            }

            var selectAlias = new TableAlias();
            var columns = new List<ColumnDeclaration>();
            var variables = new List<VariableDeclaration>();
            foreach (MemberInfo mi in members)
            {
                var col = (ColumnExpression) GetMemberExpression(tex, entity, mi);
                columns.Add(new ColumnDeclaration(mapping.GetColumnName(entity, mi), col, col.QueryType));
                var vcol = new ColumnExpression(col.Type, col.QueryType, selectAlias, col.Name);
                variables.Add(new VariableDeclaration(mi.Name, col.QueryType, vcol));
                map.Add(mi, new VariableExpression(mi.Name, col.Type, col.QueryType));
            }

            var genMembersCommand = new DeclarationCommand(variables,
                new SelectExpression(selectAlias, columns, tex, where));

            if (genIdCommand != null)
            {
                return new BlockCommand(genIdCommand, genMembersCommand);
            }

            return genMembersCommand;
        }

        private IEnumerable<ColumnAssignment> GetColumnAssignments(
            Expression table,
            Expression instance,
            MappingEntity entity,
            Func<MappingEntity, MemberInfo, bool> fnIncludeColumn,
            Dictionary<MemberInfo, Expression> map)
        {
            foreach (MemberInfo m in mapping.GetMappedMembers(entity))
            {
                if (mapping.IsColumn(entity, m) && fnIncludeColumn(entity, m))
                {
                    yield return
                        new ColumnAssignment(
                            (ColumnExpression) GetMemberExpression(table, entity, m), GetMemberAccess(instance, m, map))
                        ;
                }
                else if (mapping.IsNestedEntity(entity, m))
                {
                    IEnumerable<ColumnAssignment> assignments = GetColumnAssignments(
                        table, Expression.MakeMemberAccess(instance, m), mapping.GetRelatedEntity(entity, m),
                        fnIncludeColumn, map);
                    foreach (ColumnAssignment ca in assignments)
                    {
                        yield return ca;
                    }
                }
            }
        }

        private IEnumerable<ColumnAssignment> GetRelatedColumnAssignments(
            Expression expr, MappingEntity entity, MappingTable table, Dictionary<MemberInfo, Expression> map)
        {
            if (mapping.IsExtensionTable(table))
            {
                string[] keyColumns = mapping.GetExtensionKeyColumnNames(table).ToArray();
                MemberInfo[] relatedMembers = mapping.GetExtensionRelatedMembers(table).ToArray();
                for (int i = 0, n = keyColumns.Length; i < n; i++)
                {
                    MemberInfo member = relatedMembers[i];
                    Expression exp = map[member];
                    yield return new ColumnAssignment((ColumnExpression) GetMemberExpression(expr, entity, member), exp)
                        ;
                }
            }
        }

        private Expression GetMemberAccess(Expression instance, MemberInfo member,
            Dictionary<MemberInfo, Expression> map)
        {
            Expression exp;
            if (map == null || !map.TryGetValue(member, out exp))
            {
                exp = Expression.MakeMemberAccess(instance, member);
            }
            return exp;
        }

        public override Expression GetUpdateExpression(
            MappingEntity entity, Expression instance, LambdaExpression updateCheck, LambdaExpression selector,
            Expression @else)
        {
            IList<MappingTable> tables = mapping.GetTables(entity);
            if (tables.Count < 2)
            {
                return base.GetUpdateExpression(entity, instance, updateCheck, selector, @else);
            }

            var commands = new List<Expression>();
            foreach (MappingTable table in GetDependencyOrderedTables(entity))
            {
                var tex = new TableExpression(new TableAlias(), entity, mapping.GetTableName(table));
                IEnumerable<ColumnAssignment> assignments = GetColumnAssignments(
                    tex,
                    instance,
                    entity,
                    (e, m) => mapping.GetAlias(e, m) == mapping.GetAlias(table) && mapping.IsUpdatable(e, m),
                    null);
                Expression where = GetIdentityCheck(tex, entity, instance);
                commands.Add(new UpdateCommand(tex, where, assignments));
            }

            if (selector != null)
            {
                commands.Add(
                    new IFCommand(
                        QueryLanguage.GetRowsAffectedExpression().GreaterThan(Expression.Constant(0)),
                        GetUpdateResult(entity, instance, selector),
                        @else));
            }
            else if (@else != null)
            {
                commands.Add(
                    new IFCommand(QueryLanguage.GetRowsAffectedExpression().LessThanOrEqual(Expression.Constant(0)),
                        @else, null));
            }

            Expression block = new BlockCommand(commands);

            if (updateCheck != null)
            {
                Expression test = GetEntityStateTest(entity, instance, updateCheck);
                return new IFCommand(test, block, null);
            }

            return block;
        }

        private Expression GetIdentityCheck(
            TableExpression root, MappingEntity entity, Expression instance, MappingTable table)
        {
            if (mapping.IsExtensionTable(table))
            {
                string[] keyColNames = mapping.GetExtensionKeyColumnNames(table).ToArray();
                MemberInfo[] relatedMembers = mapping.GetExtensionRelatedMembers(table).ToArray();

                Expression where = null;
                for (int i = 0, n = keyColNames.Length; i < n; i++)
                {
                    MemberInfo relatedMember = relatedMembers[i];
                    var cex = new ColumnExpression(
                        TypeHelper.GetMemberType(relatedMember), GetColumnType(entity, relatedMember), root.Alias,
                        keyColNames[n]);
                    Expression nex = GetMemberExpression(instance, entity, relatedMember);
                    Expression eq = cex.Equal(nex);
                    where = (where != null) ? where.And(eq) : where;
                }
                return where;
            }
            return base.GetIdentityCheck(root, entity, instance);
        }

        public override Expression GetDeleteExpression(
            MappingEntity entity, Expression instance, LambdaExpression deleteCheck)
        {
            IList<MappingTable> tables = mapping.GetTables(entity);
            if (tables.Count < 2)
            {
                return base.GetDeleteExpression(entity, instance, deleteCheck);
            }

            var commands = new List<Expression>();
            foreach (MappingTable table in GetDependencyOrderedTables(entity).Reverse())
            {
                var tex = new TableExpression(new TableAlias(), entity, mapping.GetTableName(table));
                Expression where = GetIdentityCheck(tex, entity, instance);
                commands.Add(new DeleteCommand(tex, where));
            }

            Expression block = new BlockCommand(commands);

            if (deleteCheck != null)
            {
                Expression test = GetEntityStateTest(entity, instance, deleteCheck);
                return new IFCommand(test, block, null);
            }

            return block;
        }
    }
}