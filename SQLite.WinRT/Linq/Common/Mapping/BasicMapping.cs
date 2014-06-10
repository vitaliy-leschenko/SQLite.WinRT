// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.WinRT.Linq.Base;
using SQLite.WinRT.Linq.Common.Expressions;
using SQLite.WinRT.Linq.Common.Language;
using SQLite.WinRT.Linq.Common.Translation;

namespace SQLite.WinRT.Linq.Common.Mapping
{
    public abstract class BasicMapping : QueryMapping
    {
        public override MappingEntity GetEntity(Type elementType, string tableId)
        {
            if (tableId == null)
            {
                tableId = GetTableId(elementType);
            }
            return new BasicMappingEntity(elementType, tableId);
        }

        public override MappingEntity GetEntity(MemberInfo contextMember)
        {
            Type elementType = TypeHelper.GetElementType(TypeHelper.GetMemberType(contextMember));
            return GetEntity(elementType);
        }

        public override bool IsRelationship(MappingEntity entity, MemberInfo member)
        {
            return IsAssociationRelationship(entity, member);
        }

        /// <summary>
        ///     Deterimines is a property is mapped onto a column or relationship
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public virtual bool IsMapped(MappingEntity entity, MemberInfo member)
        {
            return true;
        }

        /// <summary>
        ///     Determines if a property is mapped onto a column
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public virtual bool IsColumn(MappingEntity entity, MemberInfo member)
        {
            //return this.mapping.IsMapped(entity, member) && this.translator.Linguist.Language.IsScalar(TypeHelper.GetMemberType(member));
            return IsMapped(entity, member);
        }

        /// <summary>
        ///     The type declaration for the column in the provider's syntax
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="member"></param>
        /// <returns>a string representing the type declaration or null</returns>
        public virtual string GetColumnDbType(MappingEntity entity, MemberInfo member)
        {
            return null;
        }

        /// <summary>
        ///     Determines if a property represents or is part of the entities unique identity (often primary key)
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public override bool IsPrimaryKey(MappingEntity entity, MemberInfo member)
        {
            return false;
        }

        /// <summary>
        ///     Determines if a property is computed after insert or update
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public virtual bool IsComputed(MappingEntity entity, MemberInfo member)
        {
            return false;
        }

        /// <summary>
        ///     Determines if a property is generated on the server during insert
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public virtual bool IsGenerated(MappingEntity entity, MemberInfo member)
        {
            return false;
        }

        /// <summary>
        ///     Determines if a property can be part of an update operation
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public virtual bool IsUpdatable(MappingEntity entity, MemberInfo member)
        {
            return !IsPrimaryKey(entity, member) && !IsComputed(entity, member);
        }

        /// <summary>
        ///     The type of the entity on the other side of the relationship
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public virtual MappingEntity GetRelatedEntity(MappingEntity entity, MemberInfo member)
        {
            Type relatedType = TypeHelper.GetElementType(TypeHelper.GetMemberType(member));
            return GetEntity(relatedType);
        }

        /// <summary>
        ///     Determines if the property is an assocation relationship.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public virtual bool IsAssociationRelationship(MappingEntity entity, MemberInfo member)
        {
            return false;
        }

        /// <summary>
        ///     Returns the key members on this side of the association
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public virtual IEnumerable<MemberInfo> GetAssociationKeyMembers(MappingEntity entity, MemberInfo member)
        {
            return new MemberInfo[] {};
        }

        /// <summary>
        ///     Returns the key members on the other side (related side) of the association
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public virtual IEnumerable<MemberInfo> GetAssociationRelatedKeyMembers(MappingEntity entity, MemberInfo member)
        {
            return new MemberInfo[] {};
        }

        public abstract bool IsRelationshipSource(MappingEntity entity, MemberInfo member);

        public abstract bool IsRelationshipTarget(MappingEntity entity, MemberInfo member);

        /// <summary>
        ///     The name of the corresponding database table
        /// </summary>
        /// <param name="rowType"></param>
        /// <returns></returns>
        public virtual string GetTableName(MappingEntity entity)
        {
            return entity.EntityType.Name;
        }

        /// <summary>
        ///     The name of the corresponding table column
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public virtual string GetColumnName(MappingEntity entity, MemberInfo member)
        {
            return member.Name;
        }

        /// <summary>
        ///     A sequence of all the mapped members
        /// </summary>
        /// <param name="rowType"></param>
        /// <returns></returns>
        public override IEnumerable<MemberInfo> GetMappedMembers(MappingEntity entity)
        {
            //Type type = entity.ElementType.IsInterface ? entity.EntityType : entity.ElementType;
            Type type = entity.EntityType;
            var members =
                new HashSet<MemberInfo>(
                    type.GetFields()
                        .Where(t => (t.Attributes & FieldAttributes.Public) != 0)
                        .Cast<MemberInfo>()
                        .Where(m => IsMapped(entity, m)));
            members.UnionWith(
                type.GetProperties().Where(t => t.CanWrite).Cast<MemberInfo>().Where(m => IsMapped(entity, m)));
            return members.OrderBy(m => m.Name);
        }

        public override bool IsModified(MappingEntity entity, object instance, object original)
        {
            foreach (MemberInfo mi in GetMappedMembers(entity))
            {
                if (IsColumn(entity, mi))
                {
                    if (!Equals(mi.GetValue(instance), mi.GetValue(original)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override object GetPrimaryKey(MappingEntity entity, object instance)
        {
            object firstKey = null;
            List<object> keys = null;
            foreach (MemberInfo mi in GetPrimaryKeyMembers(entity))
            {
                if (firstKey == null)
                {
                    firstKey = mi.GetValue(instance);
                }
                else
                {
                    if (keys == null)
                    {
                        keys = new List<object>();
                        keys.Add(firstKey);
                    }
                    keys.Add(mi.GetValue(instance));
                }
            }
            if (keys != null)
            {
                return new CompoundKey(keys.ToArray());
            }
            return firstKey;
        }

        public override Expression GetPrimaryKeyQuery(MappingEntity entity, Expression source, Expression[] keys)
        {
            // make predicate
            ParameterExpression p = Expression.Parameter(entity.ElementType, "p");
            Expression pred = null;
            List<MemberInfo> idMembers = GetPrimaryKeyMembers(entity).ToList();
            if (idMembers.Count != keys.Length)
            {
                throw new InvalidOperationException("Incorrect number of primary key values");
            }
            for (int i = 0, n = keys.Length; i < n; i++)
            {
                MemberInfo mem = idMembers[i];
                Type memberType = TypeHelper.GetMemberType(mem);
                if (keys[i] != null &&
                    TypeHelper.GetNonNullableType(keys[i].Type) != TypeHelper.GetNonNullableType(memberType))
                {
                    throw new InvalidOperationException("Primary key value is wrong type");
                }
                Expression eq = Expression.MakeMemberAccess(p, mem).Equal(keys[i]);
                pred = (pred == null) ? eq : pred.And(eq);
            }
            LambdaExpression predLambda = Expression.Lambda(pred, p);

            return Expression.Call(typeof (Queryable), "SingleOrDefault", new[] {entity.ElementType}, source, predLambda);
        }

        public override IEnumerable<EntityInfo> GetDependentEntities(MappingEntity entity, object instance)
        {
            foreach (MemberInfo mi in GetMappedMembers(entity))
            {
                if (IsRelationship(entity, mi) && IsRelationshipSource(entity, mi))
                {
                    MappingEntity relatedEntity = GetRelatedEntity(entity, mi);
                    object value = mi.GetValue(instance);
                    if (value != null)
                    {
                        var list = value as IList;
                        if (list != null)
                        {
                            foreach (object item in list)
                            {
                                if (item != null)
                                {
                                    yield return new EntityInfo(item, relatedEntity);
                                }
                            }
                        }
                        else
                        {
                            yield return new EntityInfo(value, relatedEntity);
                        }
                    }
                }
            }
        }

        public override IEnumerable<EntityInfo> GetDependingEntities(MappingEntity entity, object instance)
        {
            foreach (MemberInfo mi in GetMappedMembers(entity))
            {
                if (IsRelationship(entity, mi) && IsRelationshipTarget(entity, mi))
                {
                    MappingEntity relatedEntity = GetRelatedEntity(entity, mi);
                    object value = mi.GetValue(instance);
                    if (value != null)
                    {
                        var list = value as IList;
                        if (list != null)
                        {
                            foreach (object item in list)
                            {
                                if (item != null)
                                {
                                    yield return new EntityInfo(item, relatedEntity);
                                }
                            }
                        }
                        else
                        {
                            yield return new EntityInfo(value, relatedEntity);
                        }
                    }
                }
            }
        }

        public override QueryMapper CreateMapper(QueryTranslator translator)
        {
            return new BasicMapper(this, translator);
        }

        private class BasicMappingEntity : MappingEntity
        {
            private readonly string entityID;

            private readonly Type type;

            public BasicMappingEntity(Type type, string entityID)
            {
                this.entityID = entityID;
                this.type = type;
            }

            public override string TableId
            {
                get { return entityID; }
            }

            public override Type ElementType
            {
                get { return type; }
            }

            public override Type EntityType
            {
                get { return type; }
            }
        }
    }

    public class BasicMapper : QueryMapper
    {
        private readonly BasicMapping mapping;

        private readonly QueryTranslator translator;

        public BasicMapper(BasicMapping mapping, QueryTranslator translator)
        {
            this.mapping = mapping;
            this.translator = translator;
        }

        public override QueryMapping Mapping
        {
            get { return mapping; }
        }

        public override QueryTranslator Translator
        {
            get { return translator; }
        }

        /// <summary>
        ///     The query language specific type for the column
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public virtual DbQueryType GetColumnType(MappingEntity entity, MemberInfo member)
        {
            string dbType = mapping.GetColumnDbType(entity, member);
            if (dbType != null)
            {
                return DbTypeSystem.Parse(dbType);
            }
            return DbTypeSystem.GetColumnType(TypeHelper.GetMemberType(member));
        }

        public override ProjectionExpression GetQueryExpression(MappingEntity entity)
        {
            var tableAlias = new TableAlias();
            var selectAlias = new TableAlias();
            var table = new TableExpression(tableAlias, entity, mapping.GetTableName(entity));

            Expression projector = GetEntityExpression(table, entity);
            ProjectedColumns pc = ColumnProjector.ProjectColumns(projector, null, selectAlias, tableAlias);

            var proj = new ProjectionExpression(new SelectExpression(selectAlias, pc.Columns, table, null), pc.Projector);
            return proj;
            //return (ProjectionExpression)this.Translator.Police.ApplyPolicy(proj, entity.ElementType);
        }

        public override EntityExpression GetEntityExpression(Expression root, MappingEntity entity)
        {
            // must be some complex type constructed from multiple columns
            var assignments = new List<EntityAssignment>();
            foreach (MemberInfo mi in mapping.GetMappedMembers(entity))
            {
                if (!mapping.IsAssociationRelationship(entity, mi))
                {
                    Expression me = GetMemberExpression(root, entity, mi);
                    if (me != null)
                    {
                        assignments.Add(new EntityAssignment(mi, me));
                    }
                }
            }

            return new EntityExpression(entity, BuildEntityExpression(entity, assignments));
        }

        protected virtual Expression BuildEntityExpression(MappingEntity entity, IList<EntityAssignment> assignments)
        {
            NewExpression newExpression;

            // handle cases where members are not directly assignable
            EntityAssignment[] readonlyMembers = assignments.Where(b => TypeHelper.IsReadOnly(b.Member)).ToArray();
            ConstructorInfo[] cons = entity.EntityType.GetTypeInfo().DeclaredConstructors.ToArray();
            bool hasNoArgConstructor = cons.Any(c => c.GetParameters().Length == 0);

            if (readonlyMembers.Length > 0 || !hasNoArgConstructor)
            {
                // find all the constructors that bind all the read-only members
                List<ConstructorBindResult> consThatApply =
                    cons.Select(c => BindConstructor(c, readonlyMembers))
                        .Where(cbr => cbr != null && cbr.Remaining.Count == 0)
                        .
                        ToList();
                if (consThatApply.Count == 0)
                {
                    throw new InvalidOperationException(
                        string.Format("Cannot construct type '{0}' with all mapped includedMembers.", entity.ElementType));
                }
                // just use the first one... (Note: need better algorithm. :-)
                if (readonlyMembers.Length == assignments.Count)
                {
                    return consThatApply[0].Expression;
                }
                ConstructorBindResult r = BindConstructor(consThatApply[0].Expression.Constructor, assignments);

                newExpression = r.Expression;
                assignments = r.Remaining;
            }
            else
            {
                newExpression = Expression.New(entity.EntityType);
            }

            Expression result;
            if (assignments.Count > 0)
            {
                if (entity.ElementType.GetTypeInfo().IsInterface)
                {
                    assignments = MapAssignments(assignments, entity.EntityType).ToList();
                }
                result = Expression.MemberInit(
                    newExpression, assignments.Select(a => Expression.Bind(a.Member, a.Expression)).ToArray());
            }
            else
            {
                result = newExpression;
            }

            if (entity.ElementType != entity.EntityType)
            {
                result = Expression.Convert(result, entity.ElementType);
            }

            return result;
        }

        private IEnumerable<EntityAssignment> MapAssignments(IEnumerable<EntityAssignment> assignments, Type entityType)
        {
            foreach (EntityAssignment assign in assignments)
            {
                MemberInfo[] members =
                    entityType.GetTypeInfo().DeclaredMembers.Where(t => t.Name == assign.Member.Name).ToArray();
                if (members != null && members.Length > 0)
                {
                    yield return new EntityAssignment(members[0], assign.Expression);
                }
                else
                {
                    yield return assign;
                }
            }
        }

        protected virtual ConstructorBindResult BindConstructor(ConstructorInfo cons,
            IList<EntityAssignment> assignments)
        {
            ParameterInfo[] ps = cons.GetParameters();
            var args = new Expression[ps.Length];
            var mis = new MemberInfo[ps.Length];
            var members = new HashSet<EntityAssignment>(assignments);
            var used = new HashSet<EntityAssignment>();

            for (int i = 0, n = ps.Length; i < n; i++)
            {
                ParameterInfo p = ps[i];
                EntityAssignment assignment =
                    members.FirstOrDefault(
                        a => p.Name == a.Member.Name && p.ParameterType.IsAssignableFrom(a.Expression.Type));
                if (assignment == null)
                {
                    assignment =
                        members.FirstOrDefault(
                            a =>
                                string.Compare(p.Name, a.Member.Name, StringComparison.OrdinalIgnoreCase) == 0 &&
                                p.ParameterType.IsAssignableFrom(a.Expression.Type));
                }
                if (assignment != null)
                {
                    args[i] = assignment.Expression;
                    if (mis != null)
                    {
                        mis[i] = assignment.Member;
                    }
                    used.Add(assignment);
                }
                else
                {
                    MemberInfo[] mems = cons.DeclaringType.GetMember(p.Name);
                    if (mems != null && mems.Length > 0)
                    {
                        args[i] = Expression.Constant(TypeHelper.GetDefault(p.ParameterType), p.ParameterType);
                        mis[i] = mems[0];
                    }
                    else
                    {
                        // unknown parameter, does not match any member
                        return null;
                    }
                }
            }

            members.ExceptWith(used);

            return new ConstructorBindResult(Expression.New(cons, args, mis), members);
        }

        public override bool HasIncludedMembers(EntityExpression entity)
        {
            EntityPolicy policy = translator.Police.Policy;
            foreach (MemberInfo mi in mapping.GetMappedMembers(entity.Entity))
            {
                if (policy.IsIncluded(mi))
                {
                    return true;
                }
            }
            return false;
        }

        public override EntityExpression IncludeMembers(EntityExpression entity, Func<MemberInfo, bool> fnIsIncluded)
        {
            Dictionary<string, EntityAssignment> assignments =
                GetAssignments(entity.Expression).ToDictionary(ma => ma.Member.Name);
            bool anyAdded = false;
            foreach (MemberInfo mi in mapping.GetMappedMembers(entity.Entity))
            {
                EntityAssignment ea;
                bool okayToInclude = !assignments.TryGetValue(mi.Name, out ea) ||
                                     IsNullRelationshipAssignment(entity.Entity, ea);
                if (okayToInclude && fnIsIncluded(mi))
                {
                    ea = new EntityAssignment(mi, GetMemberExpression(entity.Expression, entity.Entity, mi));
                    assignments[mi.Name] = ea;
                    anyAdded = true;
                }
            }
            if (anyAdded)
            {
                return new EntityExpression(entity.Entity,
                    BuildEntityExpression(entity.Entity, assignments.Values.ToList()));
            }
            return entity;
        }

        private bool IsNullRelationshipAssignment(MappingEntity entity, EntityAssignment assignment)
        {
            if (mapping.IsRelationship(entity, assignment.Member))
            {
                var cex = assignment.Expression as ConstantExpression;
                if (cex != null && cex.Value == null)
                {
                    return true;
                }
            }
            return false;
        }

        private IEnumerable<EntityAssignment> GetAssignments(Expression newOrMemberInit)
        {
            var assignments = new List<EntityAssignment>();
            var minit = newOrMemberInit as MemberInitExpression;
            if (minit != null)
            {
                assignments.AddRange(
                    minit.Bindings.OfType<MemberAssignment>().Select(a => new EntityAssignment(a.Member, a.Expression)));
                newOrMemberInit = minit.NewExpression;
            }
            var nex = newOrMemberInit as NewExpression;
            if (nex != null && nex.Members != null)
            {
                assignments.AddRange(
                    Enumerable.Range(0, nex.Arguments.Count).Where(i => nex.Members[i] != null).Select(
                        i => new EntityAssignment(nex.Members[i], nex.Arguments[i])));
            }
            return assignments;
        }

        public override Expression GetMemberExpression(Expression root, MappingEntity entity, MemberInfo member)
        {
            if (mapping.IsAssociationRelationship(entity, member))
            {
                MappingEntity relatedEntity = mapping.GetRelatedEntity(entity, member);
                ProjectionExpression projection = GetQueryExpression(relatedEntity);

                // make where clause for joining back to 'root'
                List<MemberInfo> declaredTypeMembers = mapping.GetAssociationKeyMembers(entity, member).ToList();
                List<MemberInfo> associatedMembers = mapping.GetAssociationRelatedKeyMembers(entity, member).ToList();

                Expression where = null;
                for (int i = 0, n = associatedMembers.Count; i < n; i++)
                {
                    Expression equal =
                        GetMemberExpression(projection.Projector, relatedEntity, associatedMembers[i]).Equal(
                            GetMemberExpression(root, entity, declaredTypeMembers[i]));
                    where = (where != null) ? where.And(equal) : equal;
                }

                var newAlias = new TableAlias();
                ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, null, newAlias,
                    projection.Select.Alias);

                LambdaExpression aggregator = Aggregator.GetAggregator(
                    TypeHelper.GetMemberType(member), typeof (IEnumerable<>).MakeGenericType(pc.Projector.Type));
                var result = new ProjectionExpression(
                    new SelectExpression(newAlias, pc.Columns, projection.Select, where), pc.Projector, aggregator);

                return translator.Police.ApplyPolicy(result, member);
            }
            var aliasedRoot = root as AliasedExpression;
            if (aliasedRoot != null && mapping.IsColumn(entity, member))
            {
                return new ColumnExpression(
                    TypeHelper.GetMemberType(member),
                    GetColumnType(entity, member),
                    aliasedRoot.Alias,
                    mapping.GetColumnName(entity, member));
            }
            return QueryBinder.BindMember(root, member);
        }

        public override Expression GetInsertExpression(MappingEntity entity, Expression instance,
            LambdaExpression selector)
        {
            var tableAlias = new TableAlias();
            var table = new TableExpression(tableAlias, entity, mapping.GetTableName(entity));
            IEnumerable<ColumnAssignment> assignments = GetColumnAssignments(table, instance, entity,
                (e, m) => !mapping.IsGenerated(e, m));

            if (selector != null)
            {
                return new BlockCommand(
                    new InsertCommand(table, assignments), GetInsertResult(entity, instance, selector, null));
            }

            return new InsertCommand(table, assignments);
        }

        private IEnumerable<ColumnAssignment> GetColumnAssignments(
            Expression table, Expression instance, MappingEntity entity,
            Func<MappingEntity, MemberInfo, bool> fnIncludeColumn)
        {
            foreach (MemberInfo m in mapping.GetMappedMembers(entity))
            {
                if (mapping.IsColumn(entity, m) && fnIncludeColumn(entity, m))
                {
                    yield return
                        new ColumnAssignment(
                            (ColumnExpression) GetMemberExpression(table, entity, m),
                            Expression.MakeMemberAccess(instance, m));
                }
            }
        }

        protected virtual Expression GetInsertResult(
            MappingEntity entity, Expression instance, LambdaExpression selector, Dictionary<MemberInfo, Expression> map)
        {
            var tableAlias = new TableAlias();
            var tex = new TableExpression(tableAlias, entity, mapping.GetTableName(entity));
            LambdaExpression aggregator = Aggregator.GetAggregator(
                selector.Body.Type, typeof (IEnumerable<>).MakeGenericType(selector.Body.Type));

            Expression where;
            DeclarationCommand genIdCommand = null;
            List<MemberInfo> generatedIds =
                mapping.GetMappedMembers(entity).Where(
                    m => mapping.IsPrimaryKey(entity, m) && mapping.IsGenerated(entity, m)).ToList();
            if (generatedIds.Count > 0)
            {
                if (map == null || !generatedIds.Any(m => map.ContainsKey(m)))
                {
                    var localMap = new Dictionary<MemberInfo, Expression>();
                    genIdCommand = GetGeneratedIdCommand(entity, generatedIds.ToList(), localMap);
                    map = localMap;
                }

                // is this just a retrieval of one generated id member?
                var mex = selector.Body as MemberExpression;
                if (mex != null && mapping.IsPrimaryKey(entity, mex.Member) && mapping.IsGenerated(entity, mex.Member))
                {
                    if (genIdCommand != null)
                    {
                        // just use the select from the genIdCommand
                        return new ProjectionExpression(
                            genIdCommand.Source,
                            new ColumnExpression(
                                mex.Type, genIdCommand.Variables[0].QueryType, genIdCommand.Source.Alias,
                                genIdCommand.Source.Columns[0].Name),
                            aggregator);
                    }
                    var alias = new TableAlias();
                    DbQueryType colType = GetColumnType(entity, mex.Member);
                    return
                        new ProjectionExpression(
                            new SelectExpression(alias, new[] {new ColumnDeclaration("", map[mex.Member], colType)},
                                null, null),
                            new ColumnExpression(TypeHelper.GetMemberType(mex.Member), colType, alias, ""),
                            aggregator);
                }

                where =
                    generatedIds.Select((m, i) => GetMemberExpression(tex, entity, m).Equal(map[m]))
                        .Aggregate((x, y) => x.And(y));
            }
            else
            {
                where = GetIdentityCheck(tex, entity, instance);
            }

            Expression typeProjector = GetEntityExpression(tex, entity);
            Expression selection = DbExpressionReplacer.Replace(selector.Body, selector.Parameters[0], typeProjector);
            var newAlias = new TableAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(selection, null, newAlias, tableAlias);
            var pe = new ProjectionExpression(new SelectExpression(newAlias, pc.Columns, tex, where), pc.Projector,
                aggregator);

            if (genIdCommand != null)
            {
                return new BlockCommand(genIdCommand, pe);
            }
            return pe;
        }

        protected virtual DeclarationCommand GetGeneratedIdCommand(
            MappingEntity entity, List<MemberInfo> members, Dictionary<MemberInfo, Expression> map)
        {
            var columns = new List<ColumnDeclaration>();
            var decls = new List<VariableDeclaration>();
            var alias = new TableAlias();
            foreach (MemberInfo member in members)
            {
                Expression genId = QueryLanguage.GetGeneratedIdExpression(member);
                string name = member.Name;
                DbQueryType colType = GetColumnType(entity, member);
                columns.Add(new ColumnDeclaration(member.Name, genId, colType));
                decls.Add(
                    new VariableDeclaration(member.Name, colType,
                        new ColumnExpression(genId.Type, colType, alias, member.Name)));
                if (map != null)
                {
                    var vex = new VariableExpression(member.Name, TypeHelper.GetMemberType(member), colType);
                    map.Add(member, vex);
                }
            }
            var select = new SelectExpression(alias, columns, null, null);
            return new DeclarationCommand(decls, select);
        }

        protected virtual Expression GetIdentityCheck(Expression root, MappingEntity entity, Expression instance)
        {
            return
                mapping.GetMappedMembers(entity).Where(m => mapping.IsPrimaryKey(entity, m)).Select(
                    m => GetMemberExpression(root, entity, m).Equal(Expression.MakeMemberAccess(instance, m)))
                    .Aggregate(
                        (x, y) => x.And(y));
        }

        protected virtual Expression GetEntityExistsTest(MappingEntity entity, Expression instance)
        {
            ProjectionExpression tq = GetQueryExpression(entity);
            Expression where = GetIdentityCheck(tq.Select, entity, instance);
            return new ExistsExpression(new SelectExpression(new TableAlias(), null, tq.Select, where));
        }

        protected virtual Expression GetEntityStateTest(
            MappingEntity entity, Expression instance, LambdaExpression updateCheck)
        {
            ProjectionExpression tq = GetQueryExpression(entity);
            Expression where = GetIdentityCheck(tq.Select, entity, instance);
            Expression check = DbExpressionReplacer.Replace(updateCheck.Body, updateCheck.Parameters[0], tq.Projector);
            where = where.And(check);
            return new ExistsExpression(new SelectExpression(new TableAlias(), null, tq.Select, where));
        }

        public override Expression GetUpdateExpression(
            MappingEntity entity, Expression instance, LambdaExpression updateCheck, LambdaExpression selector,
            Expression @else)
        {
            var tableAlias = new TableAlias();
            var table = new TableExpression(tableAlias, entity, mapping.GetTableName(entity));

            Expression where = GetIdentityCheck(table, entity, instance);
            if (updateCheck != null)
            {
                Expression typeProjector = GetEntityExpression(table, entity);
                Expression pred = DbExpressionReplacer.Replace(updateCheck.Body, updateCheck.Parameters[0],
                    typeProjector);
                where = where.And(pred);
            }

            IEnumerable<ColumnAssignment> assignments = GetColumnAssignments(table, instance, entity,
                (e, m) => mapping.IsUpdatable(e, m));

            Expression update = new UpdateCommand(table, where, assignments);

            if (selector != null)
            {
                return new BlockCommand(
                    update,
                    new IFCommand(
                        QueryLanguage.GetRowsAffectedExpression().GreaterThan(Expression.Constant(0)),
                        GetUpdateResult(entity, instance, selector),
                        @else));
            }
            if (@else != null)
            {
                return new BlockCommand(
                    update,
                    new IFCommand(QueryLanguage.GetRowsAffectedExpression().LessThanOrEqual(Expression.Constant(0)),
                        @else, null));
            }
            return update;
        }

        protected virtual Expression GetUpdateResult(MappingEntity entity, Expression instance,
            LambdaExpression selector)
        {
            ProjectionExpression tq = GetQueryExpression(entity);
            Expression where = GetIdentityCheck(tq.Select, entity, instance);
            Expression selection = DbExpressionReplacer.Replace(selector.Body, selector.Parameters[0], tq.Projector);
            var newAlias = new TableAlias();
            ProjectedColumns pc = ColumnProjector.ProjectColumns(selection, null, newAlias, tq.Select.Alias);
            return new ProjectionExpression(
                new SelectExpression(newAlias, pc.Columns, tq.Select, where),
                pc.Projector,
                Aggregator.GetAggregator(selector.Body.Type, typeof (IEnumerable<>).MakeGenericType(selector.Body.Type)));
        }

        public override Expression GetInsertOrUpdateExpression(
            MappingEntity entity, Expression instance, LambdaExpression updateCheck, LambdaExpression resultSelector)
        {
            if (updateCheck != null)
            {
                Expression insert = GetInsertExpression(entity, instance, resultSelector);
                Expression update = GetUpdateExpression(entity, instance, updateCheck, resultSelector, null);
                Expression check = GetEntityExistsTest(entity, instance);
                return new IFCommand(check, update, insert);
            }
            else
            {
                Expression insert = GetInsertExpression(entity, instance, resultSelector);
                Expression update = GetUpdateExpression(entity, instance, updateCheck, resultSelector, insert);
                return update;
            }
        }

        public override Expression GetDeleteExpression(
            MappingEntity entity, Expression instance, LambdaExpression deleteCheck)
        {
            var table = new TableExpression(new TableAlias(), entity, mapping.GetTableName(entity));
            Expression where = null;

            if (instance != null)
            {
                where = GetIdentityCheck(table, entity, instance);
            }

            if (deleteCheck != null)
            {
                Expression row = GetEntityExpression(table, entity);
                Expression pred = DbExpressionReplacer.Replace(deleteCheck.Body, deleteCheck.Parameters[0], row);
                where = (where != null) ? where.And(pred) : pred;
            }

            return new DeleteCommand(table, where);
        }

        protected class ConstructorBindResult
        {
            public ConstructorBindResult(NewExpression expression, IEnumerable<EntityAssignment> remaining)
            {
                Expression = expression;
                Remaining = remaining.ToReadOnly();
            }

            public NewExpression Expression { get; private set; }

            public ReadOnlyCollection<EntityAssignment> Remaining { get; private set; }
        }

        public class EntityAssignment
        {
            public EntityAssignment(MemberInfo member, Expression expression)
            {
                Member = member;
                Debug.Assert(expression != null);
                Expression = expression;
            }

            public MemberInfo Member { get; private set; }

            public Expression Expression { get; private set; }
        }
    }
}