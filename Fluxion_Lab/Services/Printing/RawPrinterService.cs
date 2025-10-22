using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

namespace Fluxion_Lab.Services.Printing
{
    public class RawPrinterService
    {
        // ==== WinSpool API ====
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA { [MarshalAs(UnmanagedType.LPStr)] public string pDocName; [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile; [MarshalAs(UnmanagedType.LPStr)] public string pDataType; }
        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true)] static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);
        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true)] static extern bool ClosePrinter(IntPtr hPrinter);
        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true)] static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOA pDocInfo);
        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true)] static extern bool EndDocPrinter(IntPtr hPrinter);
        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true)] static extern bool StartPagePrinter(IntPtr hPrinter);
        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true)] static extern bool EndPagePrinter(IntPtr hPrinter);
        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true)] static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        // ==== Config ====
        const double PRINTABLE_WIDTH_INCHES = 6.0;
        const double LEFT_MARGIN_INCHES = 0.5;
        const int CPI = 10;
        const int BLANK_LINES_FOR_TEAR = 8;
        const int TOP_BLANK_LINES = 3;

        static readonly int LEFT_MARGIN_COLS = (int)Math.Round(LEFT_MARGIN_INCHES * CPI);
        static readonly int PAGE_COLS = (int)Math.Round(PRINTABLE_WIDTH_INCHES * CPI);

        // Table widths
        const int COL_SR = 3;
        const int COL_QTY = 7;
        const int COL_AMT = 12;
        static int COL_NAME => PAGE_COLS - (COL_SR + COL_QTY + COL_AMT);

        // ==== ESC/P helpers ====
        static string EscInit() => "\x1B@";
        static string Esc10CPI() => "\x1B\x50";        // Pica
        static string EscLineSpace16() => "\x1B\x32";  // 1/6"
        static string EscLineSpaceDots(byte n) => "\x1B\x33" + (char)n; // n/216"
        static string EscClearTabs() => "\x1B\x44\x00";
        // ESC '!' n (bit3=emphasized/bold)
        static string EscMode(byte n) => "\x1B!" + (char)n;
        static string BoldOn() => EscMode(8);
        static string BoldOff() => EscMode(0);

        // Pass printer name here (or include printData.PrinterName)
        public void PrintDotMetricsReceipt(dynamic printData, string printerName = null)
        {
            string resolvedPrinter = !string.IsNullOrWhiteSpace(printerName)
                ? printerName
                : printData?.PrinterName?.ToString();

            if (string.IsNullOrWhiteSpace(resolvedPrinter))
                throw new ArgumentException("Printer name is required. Pass it to PrintDotMetricsReceipt or set printData.PrinterName.");

            // Build header
            var header = new List<string>();
            var gs = printData.GeneralSettings?.Count > 0 ? printData.GeneralSettings[0] : null;
            if (gs != null)
            {
                if (gs.ShowFirmname == true)
                {
                    string firmName = $"{gs.FirstName} {gs.LastName}".Trim();
                    if (!string.IsNullOrWhiteSpace(firmName)) header.Add(firmName);
                }
                if (gs.ShowFirmnumber == true)
                {
                    var phones = new List<string>();
                    if (!string.IsNullOrWhiteSpace(gs.MobileNo1?.ToString())) phones.Add(gs.MobileNo1.ToString());
                    if (!string.IsNullOrWhiteSpace(gs.MobileNo2?.ToString())) phones.Add(gs.MobileNo2.ToString());
                    if (phones.Count > 0) header.Add("Ph : " + string.Join(" , ", phones));
                }
                if (gs.ShowAddress == true && !string.IsNullOrWhiteSpace(gs.AddressLine1?.ToString()))
                    header.Add(gs.AddressLine1.ToString());
                if (gs.ShowGst == true && !string.IsNullOrWhiteSpace(gs.GSTIN?.ToString()))
                    header.Add("GSTIN : " + gs.GSTIN.ToString());
                if (gs.ShowWorkingHourse == true && !string.IsNullOrWhiteSpace(gs.WorkingTime?.ToString()))
                    header.Add("Working Time : " + gs.WorkingTime.ToString());
            }
            if (header.Count == 0)
            {
                header.Add("Parambil Health Care");
                header.Add("PM ARCADE , PARAMBIL BAZAR");
                header.Add("Ph : 9656343725 , 8606343725");
                header.Add("Working Time : 24 Hours");
            }

            // Header info
            string billNumber = printData.HeaderInfo?.Count > 0 ? printData.HeaderInfo[0].OpNo?.ToString() : "";
            DateTime when = DateTime.Now;
            string patientName = printData.HeaderInfo?.Count > 0 ? printData.HeaderInfo[0].PatientName?.ToString() : "";
            string ageSex = printData.HeaderInfo?.Count > 0 ? $"{printData.HeaderInfo[0].Age}/{printData.HeaderInfo[0].Gender}" : "";
            string mobile = "";
            string refBy = printData.HeaderInfo?.Count > 0 ? printData.HeaderInfo[0].Ref_DoctorName?.ToString() : "";

            // Items
            var items = new List<(string item, int qty, decimal amt)>();
            foreach (var line in printData.LineItems)
            {
                string itemName = line.Name?.ToString() ?? "";
                int qty = line.Qty != null ? Convert.ToInt32(line.Qty) : 0;
                decimal amt = line.Rate != null ? Convert.ToDecimal(line.Rate) : 0m;
                items.Add((itemName, qty, amt));
            }

            // Totals
            decimal total = 0m, paid = 0m;
            foreach (var it in items) total += it.amt;

            if (printData.HeaderInfo != null && printData.HeaderInfo.Count > 0)
            {
                var hi = printData.HeaderInfo[0];
                if (hi.PaidAmount != null) paid = Convert.ToDecimal(hi.PaidAmount);
            }

            decimal balance = total - paid;

            string printedBy = "";
            if (!string.IsNullOrWhiteSpace(printData.HeaderInfo?[0]?.UserName?.ToString()))
                printedBy = printData.HeaderInfo[0].UserName.ToString();

            byte[] job = BuildReceipt(header.ToArray(), billNumber, when,
                                      patientName, ageSex, mobile, refBy,
                                      items, total, paid, balance, printedBy);
            SendRaw(resolvedPrinter, job);
        }

        static byte[] BuildReceipt(
            string[] headerLines,
            string billNumber,
            DateTime when,
            string patientName,
            string ageSex,
            string mobile,
            string refBy,
            List<(string item, int qty, decimal amt)> items,
            decimal total,
            decimal paid,
            decimal balance,
            string printedBy)
        {
            var sb = new StringBuilder();

            // Init printer + base settings
            sb.Append(EscInit());
            sb.Append(Esc10CPI());
            sb.Append(EscLineSpace16());
            sb.Append(EscClearTabs());

            // Top blank lines
            for (int i = 0; i < TOP_BLANK_LINES; i++) sb.Append("\r\n");

            string rule() => new string('-', PAGE_COLS);
            string padL(string s) => new string(' ', LEFT_MARGIN_COLS) + s;
            string cut(string s, int w) => s.Length <= w ? s : s.Substring(0, w);
            string left(string s, int w) => s.Length > w ? s[..w] : s + new string(' ', w - s.Length);
            string right(string s, int w) => s.Length > w ? s[^w..] : new string(' ', w - s.Length) + s;
            string center(string s)
            {
                s = cut(s, PAGE_COLS);
                int pad = Math.Max(0, (PAGE_COLS - s.Length) / 2);
                return new string(' ', LEFT_MARGIN_COLS + pad) + s;
            }
            void line(string s) => sb.Append(padL(s)).Append("\r\n");
            string ComposeLeftRight(string leftText, string rightText)
            {
                leftText = cut(leftText, PAGE_COLS);
                rightText = cut(rightText, PAGE_COLS);
                int space = PAGE_COLS - leftText.Length - rightText.Length;
                if (space < 1) space = 1;
                return new string(' ', LEFT_MARGIN_COLS) + leftText + new string(' ', space) + rightText;
            }

            // Header
            sb.Append(BoldOn());
            foreach (var h in headerLines) line(center(h));
            sb.Append(BoldOff());
            line("");

            // First rule AFTER header + title
            line(rule());
            sb.Append(EscLineSpaceDots(36));
            sb.Append(BoldOn()); line(center("CASH BILL")); sb.Append(BoldOff());
            line("");

            string dt = when.ToString("dd-MMM-yyyy h:mm tt");
            sb.Append(ComposeLeftRight($"Name: {patientName}", $"Bill Number: {billNumber}")).Append("\r\n");
            sb.Append(ComposeLeftRight($"Age/Sex: {ageSex}", $"Date: {dt}")).Append("\r\n");
            line($"Mobile: {mobile}");
            line($"Ref By: {refBy}");
            line("");

            // Table header
            line(rule());
            line(left("Sr", COL_SR) + left("Item Name", COL_NAME) + right("Qty", COL_QTY) + right("Amount", COL_AMT));
            line(rule());

            // Items (Qty + Amount bold)
            int sr = 1;
            foreach (var it in items)
            {
                sb.Append(padL(left(sr.ToString() + ".", COL_SR) + left(it.item, COL_NAME)));
                sb.Append(BoldOn());
                sb.Append(right(it.qty.ToString(), COL_QTY) + right(it.amt.ToString("0.00"), COL_AMT));
                sb.Append(BoldOff());
                sb.Append("\r\n");
                sr++;
            }

            line(rule());

            // Totals (values bold)
            void PrintLabelValue(string label, decimal value)
            {
                string valueStr = value.ToString("0.00");
                int leftWidth = PAGE_COLS - COL_AMT;
                string leftPad = new string(' ', Math.Max(0, leftWidth - label.Length));
                sb.Append(padL(leftPad + label));
                sb.Append(BoldOn());
                sb.Append(right(valueStr, COL_AMT));
                sb.Append(BoldOff());
                sb.Append("\r\n");
            }
            PrintLabelValue("Total:", total);
            PrintLabelValue("Paid Amount:", paid);
            PrintLabelValue("Balance:", balance);

            line(rule());
            line(center("Thank you for visiting!"));
            if (!string.IsNullOrWhiteSpace(printedBy)) line(center($"Printed by: {printedBy}"));
            line(rule());

            // Tear-off spacing
            for (int i = 0; i < BLANK_LINES_FOR_TEAR; i++)
                sb.Append(new string(' ', LEFT_MARGIN_COLS)).Append("\r\n");

            // OEM 437 prevents odd punctuation on some dot-matrix drivers
            return Encoding.GetEncoding(437).GetBytes(sb.ToString());
        }

        static void SendRaw(string printerName, byte[] data)
        {
            if (!OpenPrinter(printerName, out var h, IntPtr.Zero))
                throw new Exception("OpenPrinter failed: " + Marshal.GetLastWin32Error());
            try
            {
                var di = new DOCINFOA { pDocName = "Cash Bill (ESC ! bold)", pDataType = "RAW" };
                if (!StartDocPrinter(h, 1, di))
                    throw new Exception("StartDocPrinter failed: " + Marshal.GetLastWin32Error());
                try
                {
                    if (!StartPagePrinter(h))
                        throw new Exception("StartPagePrinter failed: " + Marshal.GetLastWin32Error());
                    var p = Marshal.AllocCoTaskMem(data.Length);
                    try
                    {
                        Marshal.Copy(data, 0, p, data.Length);
                        if (!WritePrinter(h, p, data.Length, out _))
                            throw new Exception("WritePrinter failed: " + Marshal.GetLastWin32Error());
                    }
                    finally { Marshal.FreeCoTaskMem(p); }
                    EndPagePrinter(h);
                }
                finally { EndDocPrinter(h); }
            }
            finally { ClosePrinter(h); }
        }
    }
}
