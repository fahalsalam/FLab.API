using Dapper;
using Fluxion_Lab.Classes.DBOperations;
using Fluxion_Lab.Models.General;
using Fluxion_Lab.Models.Print;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Data;
using System.Runtime.InteropServices;
using static Fluxion_Lab.Controllers.Authentication.AuthenticationController;

namespace Fluxion_Lab.Controllers.Print
{
    [Route("api/0208")]
    public class PrintController : ControllerBase
    {
        private readonly JwtKey _key;
        private readonly IDbConnection _dbcontext;
        protected APIResponse _response;

        public PrintController(IOptions<JwtKey> options, IDbConnection dbcontext, APIResponse response)
        {
            this._key = options.Value;
            _dbcontext = dbcontext;
            _response = response;
        }

        #region Raw Printer Send
        [HttpPost("sendRawToPrinter")]
        public IActionResult SendRawToPrinter([FromHeader] string PrinterName, [FromBody] RawPrintRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(PrinterName))
                {
                    _response.isSucess = false;
                    _response.message = "Printer name is required";
                    return BadRequest(_response);
                }

                if (request == null || string.IsNullOrEmpty(request.RawData))
                {
                    _response.isSucess = false;
                    _response.message = "Raw data is required";
                    return BadRequest(_response);
                }

                bool result = SendStringToPrinter(PrinterName, request.RawData);

                if (result)
                {
                    _response.isSucess = true;
                    _response.message = "Print job sent successfully";
                    _response.data = new { printerName = PrinterName, success = true };
                }
                else
                {
                    _response.isSucess = false;
                    _response.message = "Failed to send print job";
                    _response.data = null;
                }

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;
                return StatusCode(500, _response);
            }
        }
        #endregion

        #region P/Invoke Declarations
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDocName;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pOutputFile;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pDataType;
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOA di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);
        #endregion

        #region Print Helper Method
        public static bool SendStringToPrinter(string szPrinterName, string szString)
        {
            IntPtr hPrinter = IntPtr.Zero;
            bool success = false;

            try
            {
                DOCINFOA di = new DOCINFOA
                {
                    pDocName = "Raw Print Job",
                    pDataType = "RAW"
                };

                if (OpenPrinter(szPrinterName.Normalize(), out hPrinter, IntPtr.Zero))
                {
                    if (StartDocPrinter(hPrinter, 1, di))
                    {
                        if (StartPagePrinter(hPrinter))
                        {
                            IntPtr pBytes = Marshal.StringToCoTaskMemAnsi(szString);
                            try
                            {
                                bool written = WritePrinter(hPrinter, pBytes, szString.Length, out int dwWritten);
                                success = written && dwWritten > 0;
                            }
                            finally
                            {
                                Marshal.FreeCoTaskMem(pBytes);
                            }
                            EndPagePrinter(hPrinter);
                        }
                        EndDocPrinter(hPrinter);
                    }
                    ClosePrinter(hPrinter);
                }
            }
            catch
            {
                if (hPrinter != IntPtr.Zero)
                {
                    ClosePrinter(hPrinter);
                }
                throw;
            }

            return success;
        }
        #endregion
    }
}
