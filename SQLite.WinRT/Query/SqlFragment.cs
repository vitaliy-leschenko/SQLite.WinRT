namespace SQLite.WinRT.Query
{
    public class SqlFragment : ISqlFragment
    {
        private string and = " AND ";
        public string AND { get { return and; } set { and = value; } }

        private string @as = " AS ";
        public string AS { get { return @as; } set { @as = value; } }

        private string asc = " ASC";
        public string ASC { get { return asc; } set { asc = value; } }

        private string between = " BETWEEN ";
        public string BETWEEN { get { return between; } set { between = value; } }

        private string crossJoin = " CROSS JOIN ";
        public string CROSS_JOIN { get { return crossJoin; } set { crossJoin = value; } }

        private string deleteFrom = "DELETE FROM ";
        public string DELETE_FROM { get { return deleteFrom; } set { deleteFrom = value; } }

        private string desc = " DESC";
        public string DESC { get { return desc; } set { desc = value; } }

        private string distinct = "DISTINCT ";
        public string DISTINCT { get { return distinct; } set { distinct = value; } }

        private string equalTo = " = ";
        public string EQUAL_TO { get { return equalTo; } set { equalTo = value; } }

        private string @from = " FROM ";
        public string FROM { get { return @from; } set { @from = value; } }

        private string groupBy = " GROUP BY ";
        public string GROUP_BY { get { return groupBy; } set { groupBy = value; } }

        private string having = " HAVING ";
        public string HAVING { get { return having; } set { having = value; } }

        private string @in = " IN ";
        public string IN { get { return @in; } set { @in = value; } }

        private string innerJoin = " INNER JOIN ";
        public string INNER_JOIN { get { return innerJoin; } set { innerJoin = value; } }

        private string insertInto = "INSERT INTO ";
        public string INSERT_INTO { get { return insertInto; } set { insertInto = value; } }

        private string joinPrefix = "J";
        public string JOIN_PREFIX { get { return joinPrefix; } set { joinPrefix = value; } }

        private string leftInnerJoin = " LEFT INNER JOIN ";
        public string LEFT_INNER_JOIN { get { return leftInnerJoin; } set { leftInnerJoin = value; } }

        private string leftJoin = " LEFT JOIN ";
        public string LEFT_JOIN { get { return leftJoin; } set { leftJoin = value; } }

        private string leftOuterJoin = " LEFT OUTER JOIN ";
        public string LEFT_OUTER_JOIN { get { return leftOuterJoin; } set { leftOuterJoin = value; } }

        private string notEqualTo = " <> ";
        public string NOT_EQUAL_TO { get { return notEqualTo; } set { notEqualTo = value; } }

        private string notIn = " NOT IN ";
        public string NOT_IN { get { return notIn; } set { notIn = value; } }

        private string @on = " ON ";
        public string ON { get { return @on; } set { @on = value; } }

        private string or = " OR ";
        public string OR { get { return or; } set { or = value; } }

        private string orderBy = " ORDER BY ";
        public string ORDER_BY { get { return orderBy; } set { orderBy = value; } }

        private string outerJoin = " OUTER JOIN ";
        public string OUTER_JOIN { get { return outerJoin; } set { outerJoin = value; } }

        private string rightInnerJoin = " RIGHT INNER JOIN ";
        public string RIGHT_INNER_JOIN { get { return rightInnerJoin; } set { rightInnerJoin = value; } }

        private string rightJoin = " RIGHT JOIN ";
        public string RIGHT_JOIN { get { return rightJoin; } set { rightJoin = value; } }

        private string rightOuterJoin = " RIGHT OUTER JOIN ";
        public string RIGHT_OUTER_JOIN { get { return rightOuterJoin; } set { rightOuterJoin = value; } }

        private string @select = "SELECT ";
        public string SELECT { get { return @select; } set { @select = value; } }

        private string set = " SET ";
        public string SET { get { return set; } set { set = value; } }

        private string space = " ";
        public string SPACE { get { return space; } set { space = value; } }

        private string top = "TOP ";
        public string TOP { get { return top; } set { top = value; } }

        private string unequalJoin = " JOIN ";
        public string UNEQUAL_JOIN { get { return unequalJoin; } set { unequalJoin = value; } }

        private string update = "UPDATE ";
        public string UPDATE { get { return update; } set { update = value; } }

        private string @where = " WHERE ";
        public string WHERE { get { return @where; } set { @where = value; } }
    }
}