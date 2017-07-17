using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

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
		public SqlBuilder Expr() => New()._wmOn();

		public virtual bool DropIndexNeedsOnClause => false;

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
		public virtual SqlBuilder T(string type) => delLastComma().add(type, true, true).add(",");

		public virtual SqlBuilder Create() => start("CREATE");
		public virtual SqlBuilder Drop() => start("DROP")._dmOn();
		public virtual SqlBuilder Update(string what) => start("UPDATE").add(what, false, true).add("SET", false, true)._umOn();
		public virtual SqlBuilder InsertInto(string where) => start("INSERT INTO").add(where, false, true).openBracket()._imOn();
		public virtual SqlBuilder Delete() => start("DELETE");
		public virtual SqlBuilder Select() => start("SELECT");

		public virtual SqlBuilder C(string column) => addIfTrue(_updateMode && _updateField++ > 0, ",").add(column).add(_whereMode || _updateMode ? "" : ",");
		public SqlBuilder this[string column] => C(column);
		public virtual SqlBuilder As => delLastComma().add("AS", true, true);

		public virtual SqlBuilder IfExists() => add("IF EXISTS", true, true);
		public virtual SqlBuilder IfNotExists() => add("IF NOT EXISTS", true, true);

		public virtual SqlBuilder Table(string name) => add("TABLE").addIfTrue(_dropMode, () => _ = IfExists()).add(name, true, true).openBracketIfTrue(!_dropMode)._vmOn();

		// should be used only for column constraint(s)
		public virtual SqlBuilder CConstraint(string name)
			=> delLastComma().add("CONSTRAINT", true, true).add(name, false, true);

		// should be used only for table constraints
		public virtual SqlBuilder Constraint(string name = null)
			=> delLastComma().closeBracketIfTrue(_constraintElementMode)._cemOff().add(",").addIfTrue(name != null, "CONSTRAINT", true, true).addIfTrue(name != null, name, false, true)._tcmOn();

		public virtual SqlBuilder Null() => T("NULL");
		public virtual SqlBuilder NotNull() => T("NOT NULL");

		public virtual SqlBuilder PrimaryKey()
			=> delLastCommaIfTrue(!_tableConstraintMode).add("PRIMARY KEY", true, true).addIfTrue(!_tableConstraintMode, ",").openBracketIfTrue(_tableConstraintMode)._cemOnIfTrue(_tableConstraintMode);
		public virtual SqlBuilder Unique()
			=> delLastCommaIfTrue(!_tableConstraintMode).add("UNIQUE", true, true).addIfTrue(!_tableConstraintMode, ",").openBracketIfTrue(_tableConstraintMode)._cemOnIfTrue(_tableConstraintMode);
		public virtual SqlBuilder Check(string expr)
			=> delLastCommaIfTrue(!_tableConstraintMode).add("CHECK", true, true).openBracket().add(expr).closeBracket().add(",");
		public virtual SqlBuilder Default(string expr)
			=> delLastComma().add("DEFAULT", true, true).openBracket().add(expr).closeBracket().add(",");

		public virtual SqlBuilder Index(string name) => add("INDEX").addIfTrue(_dropMode, () => _ = IfExists()).add(name, true, true);
		public virtual SqlBuilder On(string tableName) => (_dropMode && !DropIndexNeedsOnClause ? this : add("ON", true, true).add(tableName, true, true).openBracketIfTrue(!_dropMode)._vmOn());
		public virtual SqlBuilder Asc() => delLastComma().add("ASC", true, false).add(",");
		public virtual SqlBuilder Desc() => delLastComma().add("DESC", true, false).add(",");

		public virtual SqlBuilder Values() => delLastComma().closeBracket().add("VALUES", true, true).openBracket()._vmOn();

		public virtual SqlBuilder From(string src) => delLastComma().add("FROM", true, true).add(src, false, true);

		public virtual SqlBuilder Where() => delLastComma()._wmOn().add("WHERE", true, true);
		public virtual SqlBuilder OrderBy() => _wmOff().add("ORDER BY", true, true)._obmOn();
		public virtual SqlBuilder GroupBy() => _wmOff().add("GROUP BY", true, true)._gbmOn();

		public virtual SqlBuilder M(string modifier) => add(modifier, true, true);

		public virtual SqlBuilder Ob() => delLastComma().add("(");
		public virtual SqlBuilder Cb() => delLastComma().add(")");

		public virtual SqlBuilder Eq => delLastComma().add("=");
		public virtual SqlBuilder Neq => delLastComma().add("<>");
		public virtual SqlBuilder Gt => delLastComma().add(">");
		public virtual SqlBuilder GtEq => delLastComma().add(">=");
		public virtual SqlBuilder Lt => delLastComma().add("<");
		public virtual SqlBuilder LtEq => delLastComma().add("<=");

		public virtual SqlBuilder Like => delLastComma().add("LIKE", true, true);
		public virtual SqlBuilder In => delLastComma().add("IN", true, true);
		public virtual SqlBuilder Is => delLastComma().add("IS", true, true);

		public virtual SqlBuilder And() => delLastComma().add("AND", true, true);
		public virtual SqlBuilder Or() => delLastComma().add("OR", true, true);
		public virtual SqlBuilder Not() => delLastComma().add("NOT", true, true);

		public SqlBuilder P(int num) => P("_" + num.ToString());
		public virtual SqlBuilder P(string name) => addIfTrue(_updateMode, "=").add(ParameterPrefix).add(name).addIfTrue(_valuesMode || _orderbyMode || _groupbyMode, ",");
		public virtual SqlBuilder E(string expr) => addIfTrue(_updateMode, "=").openBracket().add(expr).closeBracket().addIfTrue(_valuesMode || _orderbyMode || _groupbyMode, ",");

		public virtual string Build() => Prepare(delLastComma()._cbs()._sql.ToString());
		public static implicit operator string(SqlBuilder builder) => builder.Build();

		protected virtual string Prepare(string sql) => sql;

		#region Fields

		StringBuilder _sql = new StringBuilder(256);

		protected bool _updateMode;
		protected int _updateField;

		protected bool _insertMode;
		protected bool _valuesMode;

		protected bool _whereMode;
		protected bool _orderbyMode;
		protected bool _groupbyMode;

		protected bool _tableConstraintMode;
		protected bool _constraintElementMode;

		protected bool _dropMode;

		protected int numOfOpenBrackets;

		#endregion

		#region Tools

		protected SqlBuilder start(string arg)
		{
			_sql.Clear();

			_updateMode = false;
			_updateField = 0;

			_insertMode = false;
			_valuesMode = false;

			_whereMode = false;
			_orderbyMode = false;
			_groupbyMode = false;

			_tableConstraintMode = false;
			_constraintElementMode = false;

			_dropMode = false;

			numOfOpenBrackets = 0;

			return add(arg, false, true);
		}

		protected SqlBuilder add(string arg, bool sb = false, bool sa = false)
		{
			if (sb) _sql.Append(' ');
			_sql.Append(arg);
			if (sa) _sql.Append(' ');
			return this;
		}

		protected SqlBuilder add(bool expr, Action action)
		{
			action();
			return this;
		}

		protected SqlBuilder addIfTrue(bool expr, string arg, bool sb = false, bool sa = false)
		{
			if (expr) add(arg, sb, sa);
			return this;
		}

		protected SqlBuilder addIfTrue(bool expr, Action action)
		{
			if (expr) action();
			return this;
		}

		protected SqlBuilder delLastComma()
		{
			if (_sql.Length > 1)
				_sql.Replace(',', ' ', _sql.Length - 2, 2);
			return this;
		}

		protected SqlBuilder delLastCommaIfTrue(bool expr)
		{
			if (expr) delLastComma();
			return this;
		}

		protected SqlBuilder openBracket()
		{
			numOfOpenBrackets++;
			_sql.Append("(");
			return this;
		}

		protected SqlBuilder openBracketIfTrue(bool expr)
		{
			if (expr) openBracket();
			return this;
		}

		protected SqlBuilder closeBracket()
		{
			numOfOpenBrackets--;
			_sql.Append(")");
			return this;
		}

		protected SqlBuilder closeBracketIfTrue(bool expr)
		{
			if (expr) closeBracket();
			return this;
		}

		protected SqlBuilder _cbs()
		{
			while (numOfOpenBrackets > 0)
				closeBracket();
			return this;
		}

		protected SqlBuilder _dmOn()
		{
			_dropMode = true;
			return this;
		}

		protected SqlBuilder _tcmOn()
		{
			_tableConstraintMode = true;
			return this;
		}

		protected SqlBuilder _cemOn()
		{
			_constraintElementMode = true;
			return this;
		}

		protected SqlBuilder _cemOnIfTrue(bool expr)
		{
			if (expr) _cemOn();
			return this;
		}

		protected SqlBuilder _cemOff()
		{
			_constraintElementMode = false;
			return this;
		}

		protected SqlBuilder _umOn()
		{
			_updateMode = true;
			return this;
		}

		protected SqlBuilder _imOn()
		{
			_insertMode = true;
			return this;
		}

		protected SqlBuilder _vmOn()
		{
			_valuesMode = true;
			return this;
		}

		protected SqlBuilder _vmOff()
		{
			_valuesMode = false;
			return this;
		}

		protected SqlBuilder _wmOn()
		{
			_whereMode = true;
			_updateMode = false;
			return this;
		}

		protected SqlBuilder _wmOff()
		{
			_whereMode = false;
			return this;
		}

		protected SqlBuilder _obmOn()
		{
			_orderbyMode = true;
			return this;
		}

		protected SqlBuilder _gbmOn()
		{
			_groupbyMode = true;
			return this;
		}

		#endregion
	}
}
