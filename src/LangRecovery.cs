using System.Collections.Generic;

namespace MineManager
{
    /// <summary>
    /// Turkish strings for the recovery page. Registered from L's static constructor;
    /// English stays the fallback for anything missing here.
    /// </summary>
    internal static class LangRecovery
    {
        public static void Register(Dictionary<string, string> d)
        {
            d["Recovery"] = "Kurtarma";
            d["RECOVERY"] = "KURTARMA";
            d["System file scan & repair · one button"] = "Sistem dosyası tarama ve onarım · tek tuş";
            d["Recovery steps"] = "Kurtarma adımları";
            d["Image → system files → disk"] = "İmaj → sistem dosyaları → disk";
            d["DISM repairs the Windows image; SFC repairs protected system files; CHKDSK scans the system drive. This can take 30–60 minutes."] =
                "DISM imajı, SFC sistem dosyalarını onarır; CHKDSK diski tarar. 30–60 dakika sürebilir.";
            d["Check the image for corruption"] = "İmajda bozulma var mı kontrol et";
            d["Scan the image for damage"] = "İmajı hasar için tara";
            d["Repair the Windows image"] = "Windows imajını onar";
            d["Repair protected system files"] = "Korunan sistem dosyalarını onar";
            d["Scan the system drive"] = "Sistem diskini tara";
            d["Start all"] = "Tümünü başlat";
            d["Copy log"] = "Günlüğü kopyala";
            d["Recovery log"] = "Kurtarma günlüğü";
            d["live · last 300 lines"] = "canlı · son 300 satır";
            d["Waiting"] = "Sırada";
            d["Running…"] = "Çalışıyor…";
            d["Done"] = "Tamam";
            d["Done · restart required"] = "Tamam · yeniden başlatma gerekli";
            d["Done · errors found"] = "Tamam · hata bulundu";
            d["Stopped"] = "Durduruldu";
            d["Stopping…"] = "Durduruluyor…";
            d["Timed out after {0} min"] = "{0} dk sonra zaman aşımı";
            d["Failed · exit code {0}"] = "Başarısız · çıkış kodu {0}";
            d["Step {0}/{1} · {2}"] = "Adım {0}/{1} · {2}";
            d["▶ Step {0}/{1} · {2}"] = "▶ Adım {0}/{1} · {2}";
            d["Finished: {0} done · {1} failed · {2}"] = "Bitti: {0} tamam · {1} hata · {2}";
            d["Recovery finished"] = "Kurtarma tamamlandı";
            d["Recovery finished with problems"] = "Kurtarma sorunlarla bitti";
            d["Recovery stopped"] = "Kurtarma durduruldu";
            d["Administrator required"] = "Yönetici gerekli";
            d["Administrator rights are required for these commands."] = "Bu komutlar için yönetici hakları gerekir.";
            d["Start the recovery scan?"] = "Kurtarma taraması başlatılsın mı?";
            d["DISM, SFC and CHKDSK run one after another with administrator rights. Save your work and close other installers first. The app stays open; a restart may be needed at the end."] =
                "DISM, SFC ve CHKDSK yönetici haklarıyla sırayla çalışır. Önce işinizi kaydedin ve diğer kurulumları kapatın. Uygulama açık kalır; sonunda yeniden başlatma gerekebilir.";
            d["Start"] = "Başlat";
            d["Log copied to clipboard."] = "Günlük panoya kopyalandı.";
            d["Nothing to copy yet."] = "Henüz kopyalanacak bir şey yok.";
            d["A restart is required to finish the repair."] = "Onarımı tamamlamak için yeniden başlatma gerekiyor.";
            d["Commands: Microsoft Learn — DISM /Cleanup-Image, sfc /scannow, chkdsk /scan."] =
                "Komutlar: Microsoft Learn — DISM /Cleanup-Image, sfc /scannow, chkdsk /scan.";
            d["Report"] = "Rapor";
            d["Recovery report"] = "Kurtarma raporu";
            d["No run yet — start the scan to see the verdict for each step."] =
                "Henüz çalıştırılmadı — her adımın sonucunu görmek için taramayı başlatın.";
            d["No report yet."] = "Henüz rapor yok.";
            d["No problems"] = "Sorun yok";
            d["Repaired"] = "Onarıldı";
            d["Issues found"] = "Sorun bulundu";
            d["Failed"] = "Başarısız";
            d["Unclear"] = "Belirsiz";
            d["The step was stopped before it finished."] = "Adım bitmeden durduruldu.";
            d["The step timed out after {0} minutes."] = "Adım {0} dakika sonra zaman aşımına uğradı.";
            d["The command failed with exit code {0}."] = "Komut {0} çıkış koduyla başarısız oldu.";
            d["The command completed, but its result line was not recognised. See the log."] =
                "Komut tamamlandı ama sonuç satırı anlaşılamadı. Günlüğe bakın.";
            d["No integrity violations found. Protected system files are intact."] =
                "Bütünlük ihlali bulunamadı. Korunan sistem dosyaları sağlam.";
            d["Corrupt files were found; some could not be repaired. Check the log and the CBS log."] =
                "Bozuk dosyalar bulundu; bazıları onarılamadı. Günlüğe ve CBS günlüğüne bakın.";
            d["Corrupt files were found and repaired. Windows does not report how many files."] =
                "Bozuk dosyalar bulundu ve onarıldı. Windows kaç dosya olduğunu bildirmez.";
            d["Corrupt files were reported, but the output does not say whether they were repaired. See the log."] =
                "Bozuk dosyalar bildirildi ama onarılıp onarılmadığı yazmıyor. Günlüğe bakın.";
            d["The system file check could not run. Run the app as administrator and try again."] =
                "Sistem dosyası denetimi çalıştırılamadı. Uygulamayı yönetici olarak açıp tekrar deneyin.";
            d["No component store corruption detected. The Windows image is healthy."] =
                "Bileşen deposunda bozulma algılanmadı. Windows imajı sağlıklı.";
            d["The image repair step completed successfully."] = "İmaj onarım adımı başarıyla tamamlandı.";
            d["The image was reported as repairable and the repair step completed."] =
                "İmajın onarılabilir olduğu bildirildi ve onarım adımı tamamlandı.";
            d["Image corruption was detected. The restore step can repair it."] =
                "İmajda bozulma algılandı. Geri yükleme adımı bunu onarabilir.";
            d["The image command reported an error. See the log for the exact line."] =
                "İmaj komutu hata bildirdi. Tam satır için günlüğe bakın.";
            d["File-system problems were found. Repair needs chkdsk /f and a restart; this page only scans."] =
                "Dosya sistemi sorunları bulundu. Onarım için chkdsk /f ve yeniden başlatma gerekir; bu sayfa yalnızca tarar.";
            d["No file-system problems found on the system drive."] =
                "Sistem sürücüsünde dosya sistemi sorunu bulunamadı.";
            d["{0} repaired · {1} clean · {2} issues · {3} failed"] =
                "{0} onarıldı · {1} temiz · {2} sorun · {3} başarısız";
            d["Stopped after {0} step(s) · {1}"] = "{0} adımdan sonra durduruldu · {1}";
            d["Finished with errors · {0}"] = "Hatalarla bitti · {0}";
            d["Finished with findings · {0}"] = "Bulgularla bitti · {0}";
            d["Finished · {0} · {1} unclear"] = "Bitti · {0} · {1} belirsiz";
            d["Repair completed · {0}"] = "Onarım tamamlandı · {0}";
            d["All clean · {0} step(s) · {1}"] = "Hepsi temiz · {0} adım · {1}";
            d["{0}. {1} — {2}"] = "{0}. {1} — {2}";
            d["Result: {0}"] = "Sonuç: {0}";
            d["SFC and DISM do not report a repaired-file count; the report names the step that repaired, not a number."] =
                "SFC ve DISM onarılan dosya sayısını bildirmez; rapor sayı değil, hangi adımın onardığını söyler.";
            d["Elapsed: {0}"] = "Geçen süre: {0}";
            d["Log: {0}"] = "Günlük: {0}";
        }
    }
}
