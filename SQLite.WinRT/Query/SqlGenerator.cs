using System;
using System.Collections.Generic;
using System.Text;

namespace SQLite.WinRT.Query
{
    class SqlGenerator: ISqlGenerator
    {
        private readonly SqlQuery query;
        private readonly ISqlFragment sqlFragment = new SqlFragment();
        private readonly List<object> parameters = new List<object>();

        public List<object> Parameters
        {
            get { return parameters; }
        }

        public SqlGenerator(SqlQuery query)
        {
            this.query = query;
        }

        public string BuildUpdateStatement()
        {
            parameters.Clear();

            var sb = new StringBuilder();
            sb.Append(sqlFragment.UPDATE);
            sb.Append(query.FromTables[0]);

            for (var i = 0; i < query.SetStatements.Count; i++)
            {
                if (i == 0)
                {
                    sb.AppendLine(" ");
                    sb.Append(sqlFragment.SET);
                }
                else
                    sb.AppendLine(", ");

                sb.Append(query.SetStatements[i].ColumnName);

                sb.Append(" = ");

                if (!query.SetStatements[i].IsExpression)
                {
                    sb.Append("?");
                    parameters.Add(query.SetStatements[i].Value);
                }
                else
                    sb.Append(query.SetStatements[i].Value);
            }

            //wheres
            sb.Append(GenerateConstraints());

            return sb.ToString();
        }

        public string BuildDeleteStatement()
        {
            parameters.Clear();

            var sb = new StringBuilder();
            sb.Append(sqlFragment.DELETE_FROM);
            sb.Append(query.FromTables[0]);

            //wheres
            sb.Append(GenerateConstraints());

            return sb.ToString();
        }

        public virtual string GenerateConstraints()
        {
            var whereOperator = sqlFragment.WHERE;

            var sb = new StringBuilder();
            sb.AppendLine();

            var isFirst = true;
            foreach (var c in query.Constraints)
            {
                if (!isFirst)
                {
                    whereOperator = Enum.GetName(typeof(ConstraintType), c.Condition);
                    whereOperator = String.Concat(" ", whereOperator.ToUpper(), " ");
                }

                if (c.Comparison != Comparison.OpenParentheses && c.Comparison != Comparison.CloseParentheses)
                    sb.Append(whereOperator);

                switch (c.Comparison)
                {
                    case Comparison.BetweenAnd:
                        sb.Append(c.ColumnName);
                        sb.Append(sqlFragment.BETWEEN);
                        sb.Append("?");
                        sb.Append(sqlFragment.AND);
                        sb.Append("?");
                        Parameters.Add(c.StartValue);
                        Parameters.Add(c.EndValue);
                        break;
                    case Comparison.NotIn:
                    case Comparison.In:
                        sb.Append(c.ColumnName);
                        sb.Append(c.Comparison == Comparison.In ? sqlFragment.IN : sqlFragment.NOT_IN);
                        sb.Append("(");

                        var builder = new StringBuilder();
                        var first = true;
                        foreach (var item in c.InValues)
                        {
                            if (!first)
                                builder.Append(",");
                            else
                                first = false;

                            builder.Append("?");
                            Parameters.Add(item);
                        }

                        sb.Append(builder);
                        sb.Append(")");
                        break;
                    default:
                        sb.Append(c.ColumnName);
                        sb.Append(Constraint.GetComparisonOperator(c.Comparison));
                        if (c.Comparison == Comparison.Is || c.Comparison == Comparison.IsNot)
                        {
                            sb.Append("NULL");
                        }
                        else
                        {
                            sb.Append("?");
                            Parameters.Add(c.ParameterValue);
                        }
                        break;
                }

                isFirst = false;
            }

            return sb.ToString();
        }

    }
}
