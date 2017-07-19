using System.Text;

namespace pbXNet.Database
{
	/*
	string s = _sql
		.Select()["StorageId"]["Id"]
		.From("Things")
			.Where()
				["StorageId"].Eq.P(1)
				.And()
				["Id"].Eq.P(2)
			.OrderBy
				["Id"].E("sum(1)")["StorageId"];

	s = _sql
		.Select().E("count(*)").From(_sql.New().Select()["ID"].As["_id"]["Date"].As["_date"].From("Tst"));

	s = _sql
		.Delete().From("Things").Where().E(_sql.Expr()["StorageId"].Eq.P(1));

	// "UPDATE Things SET Data = @_3, ModifiedOn = @_4 WHERE StorageId = @_1 and Id = @_2;"

	s = _sql.
		Update("Things")
			["Data"].P(3)
			["ModifiedOn"].P(4)
		.Where
			["StorageId"].Eq.P(1)
			.And()
			["Id"].Eq.P(2);

	// INSERT INTO Things (StorageId, Id, Data, ModifiedOn) VALUES (@_1, @_2, @_3, @_4);

	s = _sql
		.InsertInto("Things")["StorageId"]["Id"]["Data"]["ModifiedOn"]
		.Values.P(1).P(2).P(3).P(4);

	s = _sql.Expr()["ModifiedOn"].Neq.E("0");

	try
	{
		s = _sql
		.Create().Table("Things2")
			["StorageId"].NVarchar(512).NotNull().CConstraint("Unique_StorageId").Unique()
			["Id"].NVarchar(256).NotNull()
			["Data"].NText().Null()
				.CConstraint("IX_Data")
					.PrimaryKey()
				.CConstraint("DataNotNull")
					.Check(_sql.Expr()["Data"].Is.NotNull().And()["Data"].Neq.E("func('a')"))
			["ModifiedOn"].T("bigint").NotNull()
				.Check(_sql.Expr["ModifiedOn"].Neq.E("0"))
			.Constraint("PK_Things2")
				.PrimaryKey()["StorageId"]["Id"]
			.Constraint()
				.Unique()["Id"]
			.Constraint()
				.Check(_sql.Expr["Id"].Neq.E("0"));

		await StatementAsync(s);
	}
	catch (Exception ex)
	{
	}

	s = _options.SqlBuilder
		.Create.Index("IX_Things_StorageId")
			.On("Things")["StorageId"];
	*/

	/// <summary>
	/// Simple auxiliary class for building SQL commands.
	/// </summary>
	public class SqlBuilder
	{
		public virtual SqlBuilder New() => new SqlBuilder();
		public virtual SqlBuilder Clone() => new SqlBuilder(this);

		public SqlBuilder Expr() => New()._wmOn();
		public SqlBuilder Expr(string expr) => New()._wmOn().E(expr);
		
		public virtual bool DropIndexStmtNeedsTableName => false;

		public virtual string ParameterPrefix => "@";

		public virtual string BooleanTypeName => "boolean";
		public virtual string SmallintTypeName => "smallint";
		public virtual string IntTypeName => "int";
		public virtual string BigintTypeName => "bigint";
		public virtual string VarcharTypeName => "varchar(?)";
		public virtual string NVarcharTypeName => "nvarchar(?)";
		public virtual string TextTypeName => "text";
		public virtual string NTextTypeName => "ntext";

		public SqlBuilder Boolean() => T(BooleanTypeName);
		public SqlBuilder Smallint() => T(SmallintTypeName);
		public SqlBuilder Int() => T(IntTypeName);
		public SqlBuilder Bigint() => T(BigintTypeName);
		public SqlBuilder Varchar(int size) => T(VarcharTypeName.Replace("?", size.ToString()));
		public SqlBuilder NVarchar(int size) => T(NVarcharTypeName.Replace("?", size.ToString()));
		public SqlBuilder Text() => T(TextTypeName);
		public SqlBuilder NText() => T(NTextTypeName);
		public virtual SqlBuilder T(string type) => DelLastComma().Add(type, true, true).Add(",");

		public virtual SqlBuilder Create() => Start("CREATE");
		public virtual SqlBuilder Drop() => Start("DROP")._dmOn();
		public virtual SqlBuilder Update(string what) => Start("UPDATE").Add(what, false, true).Add("SET", false, true)._umOn();
		public virtual SqlBuilder InsertInto(string where) => Start("INSERT INTO").Add(where, false, true).OpenBracket()._imOn();
		public virtual SqlBuilder Delete() => Start("DELETE");
		public virtual SqlBuilder Select() => Start("SELECT");

		public virtual SqlBuilder C(string column) => AddIfTrue(_updateMode && _currentColumn++ > 0, ",").Add(column).Add(_whereMode || _updateMode ? "" : ",");
		public SqlBuilder this[string column] => C(column);
		public virtual SqlBuilder As => DelLastComma().Add("AS", true, true);

		public virtual SqlBuilder IfExists() => Add("IF EXISTS", true, true);
		public virtual SqlBuilder IfNotExists() => Add("IF NOT EXISTS", true, true);

		public virtual SqlBuilder Table(string name) => Add("TABLE").AddIfTrue(_dropMode, New().IfExists()).Add(name, true, true).OpenBracketIfTrue(!_dropMode)._vmOn();

		// should be used only for column constraint(s)
		public virtual SqlBuilder CConstraint(string name)
			=> DelLastComma().Add("CONSTRAINT", true, true).Add(name, false, true);

		// should be used only for table constraints
		public virtual SqlBuilder Constraint(string name = null)
			=> DelLastComma().CloseBracketIfTrue(_tableConstraintElementMode)._tcemOff().Add(",").AddIfTrue(name != null, "CONSTRAINT", true, true).AddIfTrue(name != null, name, false, true)._tcmOn();

		public virtual SqlBuilder Null() => T("NULL");
		public virtual SqlBuilder NotNull() => T("NOT NULL");

		public virtual SqlBuilder PrimaryKey()
			=> DelLastCommaIfTrue(!_tableConstraintMode).Add("PRIMARY KEY", true, true).AddIfTrue(!_tableConstraintMode, ",").OpenBracketIfTrue(_tableConstraintMode)._tcemOnIfTrue(_tableConstraintMode);
		public virtual SqlBuilder Unique()
			=> DelLastCommaIfTrue(!_tableConstraintMode).Add("UNIQUE", true, true).AddIfTrue(!_tableConstraintMode, ",").OpenBracketIfTrue(_tableConstraintMode)._tcemOnIfTrue(_tableConstraintMode);
		public virtual SqlBuilder Check(string expr)
			=> DelLastCommaIfTrue(!_tableConstraintMode).Add("CHECK", true, true).OpenBracket().Add(expr).CloseBracket().Add(",");
		public virtual SqlBuilder Default(string expr)
			=> DelLastComma().Add("DEFAULT", true, true).OpenBracket().Add(expr).CloseBracket().Add(",");

		public virtual SqlBuilder Index(string name) => Add("INDEX").AddIfTrue(_dropMode, New().IfExists()).Add(name, true, true);
		public virtual SqlBuilder On(string tableName) => (_dropMode && !DropIndexStmtNeedsTableName ? this : Add("ON", true, true).Add(tableName, true, true).OpenBracketIfTrue(!_dropMode)._vmOn());

		public virtual SqlBuilder Asc() => DelLastComma().Add("ASC", true, false).Add(",");
		public virtual SqlBuilder Desc() => DelLastComma().Add("DESC", true, false).Add(",");

		public virtual SqlBuilder Values() => DelLastComma().CloseBracket().Add("VALUES", true, true).OpenBracket()._vmOn();

		public virtual SqlBuilder From(string src) => DelLastComma().Add("FROM", true, true).Add(src, false, true);

		public virtual SqlBuilder Where() => DelLastComma()._wmOn().Add("WHERE", true, true);
		public virtual SqlBuilder OrderBy() => _wmOff().Add("ORDER BY", true, true)._obmOn();
		public virtual SqlBuilder GroupBy() => _wmOff().Add("GROUP BY", true, true)._gbmOn();

		public virtual SqlBuilder Text(string freeText) => Add(freeText, true, true);

		public SqlBuilder P(int num) => P("_" + num.ToString());
		public virtual SqlBuilder P(string name) => AddIfTrue(_updateMode, "=").Add(ParameterPrefix).Add(name).AddIfTrue(_valuesMode || _orderbyMode || _groupbyMode, ",");

		public virtual SqlBuilder E(string expr) => AddIfTrue(_updateMode, "=").OpenBracket().Add(expr).CloseBracket().AddIfTrue(_valuesMode || _orderbyMode || _groupbyMode, ",");

		public virtual SqlBuilder Ob() => DelLastComma().Add("(");
		public virtual SqlBuilder Cb() => DelLastComma().Add(")");

		public virtual SqlBuilder Concat => DelLastComma().Add("||");
		public virtual SqlBuilder Plus => DelLastComma().Add("+");
		public virtual SqlBuilder Minus => DelLastComma().Add("-");
		public virtual SqlBuilder Multiply => DelLastComma().Add("*");
		public virtual SqlBuilder Divide => DelLastComma().Add("/");

		public virtual SqlBuilder BitwiseAnd => DelLastComma().Add("&");
		public virtual SqlBuilder BitwiseOr => DelLastComma().Add("|");

		public virtual SqlBuilder Eq => DelLastComma().Add("=");
		public virtual SqlBuilder Neq => DelLastComma().Add("<>");
		public virtual SqlBuilder Gt => DelLastComma().Add(">");
		public virtual SqlBuilder GtEq => DelLastComma().Add(">=");
		public virtual SqlBuilder Lt => DelLastComma().Add("<");
		public virtual SqlBuilder LtEq => DelLastComma().Add("<=");

		public virtual SqlBuilder Like => DelLastComma().Add("LIKE", true, true);
		public virtual SqlBuilder In => DelLastComma().Add("IN", true, true);
		public virtual SqlBuilder Is => DelLastComma().Add("IS", true, true);
		public virtual SqlBuilder Not => DelLastComma().Add("NOT", true, true);

		public virtual SqlBuilder And() => DelLastComma().Add("AND", true, true);
		public virtual SqlBuilder Or() => DelLastComma().Add("OR", true, true);

		public virtual string Build() => Prepare(DelLastComma().CloseBrackets()._sql.ToString());
		public static implicit operator string(SqlBuilder builder) => builder.Build();

		protected virtual string Prepare(string sql) => sql;

		#region Fields

		protected StringBuilder _sql;

		protected int _currentColumn;

		protected bool _dropMode;

		protected bool _updateMode;

		protected bool _insertMode;
		protected bool _valuesMode;

		protected bool _whereMode;
		protected bool _orderbyMode;
		protected bool _groupbyMode;

		protected bool _tableConstraintMode;
		protected bool _tableConstraintElementMode;

		protected int _numOfOpenBrackets;

		#endregion

		#region Tools

		public SqlBuilder()
		{
			_sql = new StringBuilder(256);
		}

		public SqlBuilder(SqlBuilder src)
		{
			_sql = new StringBuilder(src._sql.ToString());
			_currentColumn = src._currentColumn;
			_dropMode = src._dropMode;
			_updateMode = src._updateMode;
			_insertMode = src._insertMode;
			_valuesMode = src._valuesMode;
			_whereMode = src._whereMode;
			_orderbyMode = src._orderbyMode;
			_groupbyMode = src._groupbyMode;
			_tableConstraintMode = src._tableConstraintMode;
			_tableConstraintElementMode = src._tableConstraintElementMode;
			_numOfOpenBrackets = src._numOfOpenBrackets;
		}

		protected SqlBuilder Start(string arg)
		{
			_sql.Clear();
			_currentColumn = 0;
			_dropMode = false;
			_updateMode = false;
			_insertMode = false;
			_valuesMode = false;
			_whereMode = false;
			_orderbyMode = false;
			_groupbyMode = false;
			_tableConstraintMode = false;
			_tableConstraintElementMode = false;
			_numOfOpenBrackets = 0;

			return Add(arg, false, true);
		}

		protected SqlBuilder Add(string arg, bool sb = false, bool sa = false)
		{
			if (sb) _sql.Append(' ');
			_sql.Append(arg);
			if (sa) _sql.Append(' ');
			return this;
		}

		protected SqlBuilder AddIfTrue(bool expr, string arg, bool sb = false, bool sa = false)
		{
			if (expr) Add(arg, sb, sa);
			return this;
		}

		protected SqlBuilder DelLastComma()
		{
			if (_sql.Length > 1)
				_sql.Replace(',', ' ', _sql.Length - 2, 2);
			return this;
		}

		protected SqlBuilder DelLastCommaIfTrue(bool expr)
		{
			if (expr) DelLastComma();
			return this;
		}

		protected SqlBuilder OpenBracket()
		{
			_numOfOpenBrackets++;
			_sql.Append("(");
			return this;
		}

		protected SqlBuilder OpenBracketIfTrue(bool expr)
		{
			if (expr) OpenBracket();
			return this;
		}

		protected SqlBuilder CloseBracket()
		{
			_numOfOpenBrackets--;
			_sql.Append(")");
			return this;
		}

		protected SqlBuilder CloseBracketIfTrue(bool expr)
		{
			if (expr) CloseBracket();
			return this;
		}

		protected SqlBuilder CloseBrackets()
		{
			while (_numOfOpenBrackets > 0)
				CloseBracket();
			return this;
		}

		protected SqlBuilder OnOff(ref bool what, bool on)
		{
			what = on;
			return this;
		}

		protected SqlBuilder OnOffIfTrue(bool expr, ref bool what, bool on)
		{
			if (expr) what = on;
			return this;
		}

		protected SqlBuilder _dmOff() => OnOff(ref _dropMode, false);
		protected SqlBuilder _dmOn() => OnOff(ref _dropMode, true);

		protected SqlBuilder _tcmOff() => OnOff(ref _tableConstraintMode, false);
		protected SqlBuilder _tcmOn() => OnOff(ref _tableConstraintMode, true);

		protected SqlBuilder _tcemOff() => OnOff(ref _tableConstraintElementMode, false);
		protected SqlBuilder _tcemOn() => OnOff(ref _tableConstraintElementMode, true);
		protected SqlBuilder _tcemOnIfTrue(bool expr) => OnOffIfTrue(expr, ref _tableConstraintElementMode, true);

		protected SqlBuilder _umOff() => OnOff(ref _updateMode, false);
		protected SqlBuilder _umOn()
		{
			_updateMode = true;
			_currentColumn = 0;
			return this;
		}

		protected SqlBuilder _imOff() => OnOff(ref _insertMode, false);
		protected SqlBuilder _imOn() => OnOff(ref _insertMode, true);

		protected SqlBuilder _vmOff() => OnOff(ref _valuesMode, false);
		protected SqlBuilder _vmOn() => OnOff(ref _valuesMode, true);

		protected SqlBuilder _wmOff() => OnOff(ref _whereMode, false);
		protected SqlBuilder _wmOn()
		{
			_whereMode = true;
			_updateMode = false;
			return this;
		}

		protected SqlBuilder _obmOn() => OnOff(ref _orderbyMode, true);
		protected SqlBuilder _gbmOn() => OnOff(ref _groupbyMode, true);

		#endregion
	}
}
