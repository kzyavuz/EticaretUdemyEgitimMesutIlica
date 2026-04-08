using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using WebUI.Controllers;
using WebUI.Helper;

namespace WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminBaseController : BaseController
    {
        protected IActionResult DownloadSampleExcel(string fileName, (string Name, bool Required)[] columns, object[] sampleRow)
        {
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Örnek");

            // 1. satır: başlıklar
            for (int i = 0; i < columns.Length; i++)
            {
                var cell = ws.Cells[1, i + 1];
                cell.Value = columns[i].Name;
                cell.Style.Font.Bold = true;
            }

            // 2. satır: örnek veri
            for (int i = 0; i < sampleRow.Length && i < columns.Length; i++)
            {
                var cell = ws.Cells[2, i + 1];
                cell.Value = sampleRow[i];
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            return File(package.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        protected async Task<IActionResult> ExcelImport(
            IFormFile file,
            Func<IFormFile, Task<(int successCount, int updateCount, List<string> errors)>> importFunc,
            Func<int, int, string> successMessage)
        {
            if (file == null || file.Length == 0)
            {
                SetSweetAlertMessage("Hata", "Lütfen Excel dosyası seçin", "error");
                return Ok(new { success = false, message = "Dosya seçilmedi" });
            }

            var (successCount, updateCount, errors) = await importFunc(file);

            SetSweetAlertMessage("Başarılı", successMessage(successCount, updateCount), "success");

            if (errors.Any())
                SetSweetAlertMessage("Hata", string.Join("\n", errors.Take(10)), "error");

            return Ok(new { success = true, successCount, updateCount, errors });
        }

        /// <summary>
        /// Dosya yükleme veya URL çözümleme.
        /// Dosya seçildiyse yükler ve path döner.
        /// Yükleme başarısızsa null döner (controller View'a dönmeli).
        /// Sadece URL varsa onu döner.
        /// İkisi de yoksa null döner (mevcut değer korunmalı).
        /// </summary>
        protected async Task<(string? path, bool uploadFailed)> ResolveImageAsync(
            IFormFile? file, string? imageUrl, FileHelper fileHelper, string uploadPath)
        {
            if (file != null)
            {
                string? uploaded = await fileHelper.UploadAsync(file, uploadPath);
                return uploaded == null ? (null, true) : (uploaded, false);
            }

            if (!string.IsNullOrWhiteSpace(imageUrl))
                return (imageUrl, false);

            return (null, false);
        }

        /// <summary>
        /// Edit işlemlerinde mevcut dosyayı siler, yenisini yükler.
        /// deleteFlag=true ise mevcut dosyayı siler ve null döner.
        /// </summary>
        protected async Task<(string? path, bool uploadFailed)> ResolveImageOnEditAsync(
            IFormFile? file, string? imageUrl, string? currentPath,
            bool deleteFlag, FileHelper fileHelper, string uploadPath)
        {
            if (file != null)
            {
                string? uploaded = await fileHelper.UpdateAsync(file, uploadPath, currentPath);
                return uploaded == null ? (null, true) : (uploaded, false);
            }

            if (!string.IsNullOrWhiteSpace(imageUrl))
                return (imageUrl, false);

            if (deleteFlag && !string.IsNullOrEmpty(currentPath))
            {
                fileHelper.Delete(currentPath);
                return (null, false);
            }

            return (currentPath, false); // değişiklik yok, mevcut path korunur
        }
    }
}
