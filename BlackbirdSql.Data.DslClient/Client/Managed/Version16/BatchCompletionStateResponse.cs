﻿/*
 *    The contents of this file are subject to the Initial
 *    Developer's Public License Version 1.0 (the "License");
 *    you may not use this file except in compliance with the
 *    License. You may obtain a copy of the License at
 *    https://github.com/BlackbirdSQL/NETProvider/master/license.txt.
 *
 *    Software distributed under the License is distributed on
 *    an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either
 *    express or implied. See the License for the specific
 *    language governing rights and limitations under the License.
 *
 *    All Rights Reserved.
 */

//$OriginalAuthors = Jiri Cincura (jiri@cincura.net)

using BlackbirdSql.Data.Common;

namespace BlackbirdSql.Data.Client.Managed.Version16;

internal class BatchCompletionStateResponse : IResponse
{
	public short StatementHandle { get; }
	public int ProcessedMessages { get; }
	public int[] UpdatedRecordsPerMessage { get; }
	public (int, IscException)[] DetailedErrors { get; }
	public int[] AdditionalErrorsPerMessage { get; }

	public BatchCompletionStateResponse(short statementHandle, int processedMessages, int[] updatedRecordsPerMessage, (int, IscException)[] detailedErrors, int[] errorsPerMessage)
	{
		StatementHandle = statementHandle;
		ProcessedMessages = processedMessages;
		UpdatedRecordsPerMessage = updatedRecordsPerMessage;
		DetailedErrors = detailedErrors;
		AdditionalErrorsPerMessage = errorsPerMessage;
	}
}
