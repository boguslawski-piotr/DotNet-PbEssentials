using System;
using System.Collections.Generic;
using System.Text;

namespace pbXNet.Database
{
	public class SqlServerSqlBuilder : SqlBuilder
	{
		public SqlServerSqlBuilder()
			: base()
		{ }

		protected SqlServerSqlBuilder(SqlServerSqlBuilder src)
			: base(src)
		{ }

		public override SqlBuilder New() => new SqlServerSqlBuilder();
		public override SqlBuilder Clone() => new SqlServerSqlBuilder(this);

		public override string BooleanTypeName => "bit";
		public override string TextTypeName => "varchar(max)";
		public override string NTextTypeName => "nvarchar(max)";

		public override SqlBuilder Concat => ((SqlServerSqlBuilder)DelLastComma()).Add("+");

		public override bool DropIndexStmtNeedsTableName => true;
	}
}
