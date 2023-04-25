#include "pch.h"
#include "TWhereParser.h"




namespace BlackbirdDsl {




StringCell^ TWhereParser::Parse(StringCell^ root)
{
	StringCell^ parserNode = root[_Key];

	if (parserNode->Count == 0)
	{
		root->Remove(_Key);
		return root;
	}





	root[_Key] = parserNode;

	return root;
}

}