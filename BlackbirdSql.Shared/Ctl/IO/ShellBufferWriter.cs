﻿// Microsoft.VisualStudio.Data.Tools.SqlEditor, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// Microsoft.VisualStudio.Data.Tools.SqlEditor.UI.ResultPane.ShellBufferWriter

using System;
using System.Collections;
using System.IO;
using System.Text;
using BlackbirdSql.Shared.Interfaces;
using Microsoft.VisualStudio.TextManager.Interop;



namespace BlackbirdSql.Shared.Ctl.IO;


public sealed class ShellBufferWriter : AbstractResultsWriter
{
	private class Marker(int position, int length)
	{

		public Marker(int position, int length, int errorLine, IBsTextSpan textSpan)
			: this(position, length)
		{
			this.errorLine = errorLine;
			this.textSpan = textSpan;
		}



		private readonly int position = position;
		private readonly int length = length;

		private readonly int errorLine = -1;
		private readonly IBsTextSpan textSpan = null;


		public int Position => position;
		public int Length => length;
		public int ErrorLine => errorLine;
		public IBsTextSpan TextSpan => textSpan;
	}

	public sealed class ShellBufferTextWriter(ShellBufferWriter owner) : TextWriter
	{
		private readonly ShellBufferWriter owner = owner;



		public override Encoding Encoding => Encoding.Default;



		public override void Flush()
		{
			owner?.Flush();

			base.Flush();
		}


		public override void Write(char ch)
		{
			owner.Write(ch);
		}
	}

	private ShellTextBuffer buffer;

	private StringBuilder stringBuilder = new StringBuilder();

	private readonly ArrayList errorMarkers = [];

	private IVsTextView doubleClickView;

	private bool recreateStringBulderOnReset;

	public bool ReleaseStringBuilderOnFlush
	{
		get
		{
			return recreateStringBulderOnReset;
		}
		set
		{
			recreateStringBulderOnReset = value;
		}
	}

	public IVsTextView DoubleClickView
	{
		get
		{
			return doubleClickView;
		}
		set
		{
			doubleClickView = value;
		}
	}

	public override TextWriter TextWriter => new ShellBufferTextWriter(this);

	public ShellBufferWriter()
	{
	}

	public ShellBufferWriter(ShellTextBuffer buffer)
	{
		this.buffer = buffer;
	}

	public ShellBufferWriter(ShellTextBuffer buffer, IVsTextView doubleClickView)
	{
		this.buffer = buffer;
		this.doubleClickView = doubleClickView;
	}

	public void AttachTo(ShellTextBuffer buffer)
	{
		lock (this)
		{
			this.buffer = buffer;
		}
	}

	private void Write(char ch)
	{
		stringBuilder.Append(ch);
	}

	private void AppendCommon(string text, bool noCRLF)
	{
		if (noCRLF || text.EndsWith("\r\n", StringComparison.Ordinal))
		{
			stringBuilder.Append(text);
		}
		else
		{
			stringBuilder.AppendLine(text);
		}
	}

	public override void AppendNormal(string text, bool noCRLF)
	{
		lock (this)
		{
			AppendCommon(text, noCRLF);
		}
	}

	public override void AppendError(string text, bool noCRLF)
	{
		lock (this)
		{
			errorMarkers.Add(new Marker(stringBuilder.Length, text.Length));
			AppendCommon(text, noCRLF);
		}
	}

	public override void AppendError(string text, int line, IBsTextSpan textSpan, bool noCRLF)
	{
		lock (this)
		{
			errorMarkers.Add(new Marker(stringBuilder.Length, text.Length, line, textSpan));
			AppendCommon(text, noCRLF);
		}
	}

	public override void AppendWarning(string text, bool noCRLF)
	{
		lock (this)
		{
			AppendCommon(text, noCRLF);
		}
	}

	public override void Flush()
	{
		lock (this)
		{
			if (stringBuilder.Length <= 0)
			{
				return;
			}

			int textLength = buffer.TextLength;
			string text = stringBuilder.ToString();
			stringBuilder.Length = 0;
			stringBuilder.Capacity = 0;
			buffer.ReplaceText(textLength, 0, text);
			foreach (Marker errorMarker in errorMarkers)
			{
				buffer.CreateStreamMarker(1, errorMarker.Position + textLength, errorMarker.Length, errorMarker.ErrorLine, (TextSpanX)(object)errorMarker.TextSpan);
			}

			Reset();
		}
	}

	public override void Reset()
	{
		lock (this)
		{
			if (recreateStringBulderOnReset)
			{
				stringBuilder = new StringBuilder();
			}
			else
			{
				stringBuilder.Length = 0;
				stringBuilder.Capacity = 0;
			}

			errorMarkers.Clear();
		}
	}
}
