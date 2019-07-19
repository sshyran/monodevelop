using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

using Mono.Debugging.Client;

using VsStackFrame = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrame;
using VsFormat = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages.StackFrameFormat;

namespace MonoDevelop.Debugger.VsCodeDebugProtocol
{
	class VsCodeStackFrame : Mono.Debugging.Client.StackFrame
	{
		public static VsFormat GetStackFrameFormat (EvaluationOptions evalOptions)
		{
			return new VsFormat {
				Parameters = evalOptions.StackFrameFormat.ParameterTypes || evalOptions.StackFrameFormat.ParameterNames || evalOptions.StackFrameFormat.ParameterValues,
				ParameterTypes = evalOptions.StackFrameFormat.ParameterTypes,
				ParameterNames = evalOptions.StackFrameFormat.ParameterNames,
				ParameterValues = evalOptions.StackFrameFormat.ParameterValues,
				Line = evalOptions.StackFrameFormat.Line,
				Module = evalOptions.StackFrameFormat.Module
			};
		}

		static string GetLanguage (string path)
		{
			if (string.IsNullOrEmpty (path))
				return null;
			if (path.EndsWith (".cs", StringComparison.OrdinalIgnoreCase))
				return "C#";
			if (path.EndsWith (".fs", StringComparison.OrdinalIgnoreCase))
				return "F#";
			if (path.EndsWith (".vb", StringComparison.OrdinalIgnoreCase))
				return "VB";
			return null;
		}

		static SourceLink GetSourceLink(VSSourceLinkInfo info)
		{
			if (info == null)
				return null;
			return new SourceLink (info.Url, info.RelativeFilePath);
		}

		static SourceLocation GetSourceLocation (VsStackFrame frame)
		{
			var sourceLink = GetSourceLink (frame.Source.VsSourceLinkInfo);
			return new SourceLocation (frame.Name, frame.Source?.Path, frame.Line, frame.Column, frame.EndLine ?? -1, frame.EndColumn ?? -1, GetHashBytes (frame.Source), sourceLink);
		}

		VsFormat format;
		readonly int threadId;
		readonly int frameIndex;
		internal readonly int frameId;
		string fullStackframeText;

		public VsCodeStackFrame (VsFormat format, int threadId, int frameIndex, VsStackFrame frame)
			: base (0, GetSourceLocation (frame), GetLanguage (frame.Source?.Path))
		{
			this.format = format;
			this.threadId = threadId;
			this.frameIndex = frameIndex;
			this.fullStackframeText = frame.Name;
			this.frameId = frame.Id;
		}

		static byte [] HexToByteArray (string hex)
		{
			if (hex.Length % 2 == 1)
				throw new ArgumentException ();
			byte [] bytes = new byte [hex.Length / 2];
			for (int i = 0; i < bytes.Length; i++) {
				bytes [i] = Convert.ToByte (hex.Substring (i * 2, 2), 16);
			}
			return bytes;
		}

		static bool ChecksumAlgorithmSupported(ChecksumAlgorithm algorithm)
		{
			return algorithm == ChecksumAlgorithm.SHA1 || algorithm == ChecksumAlgorithm.SHA256 || algorithm == ChecksumAlgorithm.MD5;
		}

		static byte [] GetHashBytes (Source source)
		{
			if (source == null)
				return null;
			var checkSum = source.Checksums.FirstOrDefault (c => ChecksumAlgorithmSupported (c.Algorithm));
			if (checkSum == null)
				return null;
			return HexToByteArray (checkSum.ChecksumValue);
		}

		public override string FullStackframeText {
			get {
				//If StackFrameFormat changed since last fetch, refeatch
				var currentFormat = GetStackFrameFormat (DebuggerSession.EvaluationOptions);
				if (currentFormat.Hex != format.Hex ||
					currentFormat.Line != format.Line ||
					currentFormat.Module != format.Module ||
					currentFormat.Parameters != format.Parameters ||
					currentFormat.ParameterNames != format.ParameterNames ||
					currentFormat.ParameterTypes != format.ParameterTypes ||
					currentFormat.ParameterValues != format.ParameterValues) {
					format = currentFormat;
					var body = ((VSCodeDebuggerSession)DebuggerSession).protocolClient.SendRequestSync (new StackTraceRequest (threadId) { StartFrame = frameIndex, Levels = 1, Format = currentFormat });
					fullStackframeText = body.StackFrames [0].Name;
				}
				return fullStackframeText;
			}
		}
	}
}