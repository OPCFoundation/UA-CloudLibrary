// #define INCLUDE_TIMESTAMP
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AdminShell
{
    /// <summary>
    /// FunctionCall - A minimally invasive function that grabs details about the caller
    /// and sends that information to the Visual Studio "Output" window.
    /// 
    /// To use, embed the following line:
    ///      FunctionCalled.LogCall3();
    ///     
    /// Or to include the namespace, when working outside the AdminShell namespace, use this line:
    ///      AdminShell.FunctionCalled.LogCall3();
    /// 
    /// 
    /// </summary>
    public class FunctionCalled
    {
        private const string strTrimFromFilePath = @"C:\Github.OpcFoundation\UA-CloudLibrary_erichb_aas_rest";

        /// <summary>
        /// LogCall3()
        /// 
        /// To use, embed the following line:
        ///      FunctionCalled.LogCall3();
        /// 
        /// </summary>
        /// <param name="strFunctionName">Auto-generated string.</param>
        /// <param name="strSourceFilePath">Auto-generated string.</param>
        /// <param name="iSourceFileLine">Auto-generated string.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void LogCall3(
            [CallerMemberName] string strFunctionName = "",
            [CallerFilePath] string strSourceFilePath = "",
            [CallerLineNumber] int iSourceFileLine = 0)
        {
            // Get the calling class name using the stack trace
            string strCallerClass = "UnknownClass";
            string strParameters = "()";
            var stackTrace = new StackTrace();
            if (stackTrace.FrameCount > 1)
            {
                // Try getting namespace + class and parameters
                var method = stackTrace.GetFrame(1)?.GetMethod();
                if (method?.DeclaringType != null)
                {
                    // Class (along with namespace)
                    strCallerClass = method.DeclaringType.FullName ?? "<<UnknownClass>>";

                    // Get parameter details.
                    var ap = method?.GetParameters();
                    if (ap != null && ap.Length > 0)
                    {
                        string[] aparms = ap.Select(ap => $"{ap.ParameterType.Name} {ap.Name}").ToArray();
                        strParameters = $"({string.Join(", ", aparms)})";
                    }
                }
            }

            strSourceFilePath = strSourceFilePath.Replace(strTrimFromFilePath, ".", StringComparison.CurrentCulture);

#if INCLUDE_TIMESTAMP
            string strTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Debug_WriteLine($"FUNCTION CALLED: {strTimeStamp} {strCallerClass}.{strFunctionName}{strParameters} in {strSourceFilePath}({iSourceFileLine})");
#else
            Debug_WriteLine($"FUNCTION CALLED: {strCallerClass}.{strFunctionName}{strParameters} in {strSourceFilePath}({iSourceFileLine})");
#endif
        }
        /// <summary>
        /// FunctionCall - Another minimally invasive function that grabs details about the caller
        /// and sends that information to the Visual Studio "Output" window.
        /// 
        /// To use, embed the following line:
        /// Embed this with
        ///
        ///        FunctionCalled.LogCall2();
        /// </summary>
        /// <param name="strFunctionName">Auto-generated string.</param>
        /// <param name="strSourceFilePath">Auto-generated string.</param>
        /// <param name="iSourceFileLine">Auto-generated string.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void LogCall2(
            [CallerMemberName] string strFunctionName = "",
            [CallerFilePath] string strSourceFilePath = "",
            [CallerLineNumber] int iSourceFileLine = 0)
        {
            // Get the calling class name using the stack trace
            string strCallerClass = "UnknownClass";
            var stackTrace = new StackTrace();
            if (stackTrace.FrameCount > 1)
            {
                var method = stackTrace.GetFrame(1)?.GetMethod();
                if (method?.DeclaringType != null)
                {
                    strCallerClass = method.DeclaringType.FullName ?? "<<UnknownClass>>";
                }
            }

            strSourceFilePath = strSourceFilePath.Replace(strTrimFromFilePath, ".", 0);

#if INCLUDE_TIMESTAMP
            string strTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Debug_WriteLine($"FUNCTION CALLED: {strTimeStamp} {strCallerClass}.{strFunctionName} in {strSourceFilePath}({iSourceFileLine})");
#else
            Debug_WriteLine($"FUNCTION CALLED: {strCallerClass}.{strFunctionName} in {strSourceFilePath}({iSourceFileLine})");
#endif
        }


        /// <summary>
        /// LogCall - the least invasive version of all. Just displays namespace/class/function
        /// name, without any reference to source file or source line number.
        /// </summary>
        /// <param name="strFunctionName">Auto-generated string.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void LogCall(
            [CallerMemberName] string strFunctionName = "")
        {
            // Get the calling class name using the stack trace
            string strCallerClass = "UnknownClass";
            var stackTrace = new StackTrace();
            if (stackTrace.FrameCount > 1)
            {
                var method = stackTrace.GetFrame(1)?.GetMethod();
                if (method?.DeclaringType != null)
                {
                    strCallerClass = method.DeclaringType.FullName ?? "<<UnknownClass>>";
                }
            }

#if INCLUDE_TIMESTAMP
            string strTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Debug_WriteLine($"FUNCTION CALLED: {strTimeStamp} {strCallerClass}.{strFunctionName}");
#else
            Debug_WriteLine($"FUNCTION CALLED: {strCallerClass}.{strFunctionName}");
#endif
        }

        private static void Debug_WriteLine(string strLine)
        {
            System.Diagnostics.Debug.WriteLine(strLine);
        }

    } // class
} // namespace


