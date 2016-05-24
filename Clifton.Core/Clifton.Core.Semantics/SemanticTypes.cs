﻿using System;

using Clifton.Core.ServiceInterfaces;

namespace Clifton.Core.Semantics
{
	public class ST_Exception : ISemanticType
	{
		public Exception Exception { get; set; }

		public ST_Exception()
		{
		}

		public ST_Exception(Exception ex)
		{
			Exception = ex;
		}
	}

	public class ST_ExceptionObject : ISemanticType
	{
		public ExceptionMessage ExceptionMessage { get; set; }

		public ST_ExceptionObject()
		{
		}

		public ST_ExceptionObject(ExceptionMessage ex)
		{
			ExceptionMessage = ex;
		}
	}

	/// <summary>
	/// Used by TemplateEngine and other runtime compiler services.
	/// </summary>
	public class ST_CompilerError : ISemanticType
	{
		public string Error {get; set;}

		public ST_CompilerError()
		{
		}

		public ST_CompilerError(string err)
		{
			Error = err;
		}
	}

	public class ST_Log : ISemanticType
	{
		public string Message { get; set; }

		public ST_Log()
		{
		}
	}

	public class XmlFileName : ImmutableSemanticType<XmlFileName, string> { };
	public class FullPath : ImmutableSemanticType<FullPath, string> { };
	public class AssemblyFileName : ImmutableSemanticType<AssemblyFileName, string> { }

	public class ConnectionString : ImmutableSemanticType<ConnectionString, string> { }
	public class UserName : ImmutableSemanticType<UserName, string> { }
	public class HashedPassword : ImmutableSemanticType<HashedPassword, string> { }
	public class PlainTextPassword : ImmutableSemanticType<PlainTextPassword, string> { }
	public class UserId : ImmutableSemanticType<UserId, int> { }
	public class ViewName : ImmutableSemanticType<ViewName, string> { }
	public class WhereClause : ImmutableSemanticType<WhereClause, string> { }
}
