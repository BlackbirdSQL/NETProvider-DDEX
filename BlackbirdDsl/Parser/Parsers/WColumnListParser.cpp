#include "pch.h"
#include "WColumnListParser.h"




namespace BlackbirdDsl {


StringCell^ WColumnListParser::Execute(SysStr^ sql)
{
	array<SysStr^>^ columns = sql->Split(',');

	StringCell^ cols = NullCell;

	for each (SysStr ^ v in columns)
	{
		cols->Add(CellPairs((CellPair("expr_type", Expressions::COLREF), CellPair("base_expr", v->Trim()),
			CellPair("no_quotes", ExtractQuotesPairs(v)))));
	}

	return cols;

}



}