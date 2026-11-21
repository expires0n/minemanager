# MineManager — Windows 7 Aero (2007–2012) tema sistemi

Bu dosya temanın **tek karar kaynağıdır**. Token değerleri
`tokens.json` → `aero-tokens.css` (üretilen) → `src/Theme.cs` (`Aero` sınıfı)
zinciriyle taşınır.

## 1. Stack ve kapsam

| | |
|---|---|
| Uygulama | MineManager — tek EXE, portable Windows sistem aracı |
| Stack | **C# 5 / WinForms / .NET Framework 4.8**, `csc.exe` ile derlenir |
| UI teknolojisi | Tamamen özel çizim (GDI+) — Electron, Tauri, web, Tailwind, shadcn, CSS-in-JS **yok** |
| Tema seçenekleri | Açık Aero (varsayılan) + koyu graphite Aero; dil İngilizce (varsayılan) / Türkçe |
| Yeni tema | Aero: cam çerçeve + parlak gradyan + 1px beyaz iç çizgi + dar ölçüler |
| Token kaynağı | `design-system/minemanager/tokens.json` (primitive → semantic → component) |
| Üretilen CSS | `design-system/minemanager/aero-tokens.css` (`ui-styling`/Tailwind yok; handoff + QA için) |
| C# aynası | `src/Theme.cs` → `Aero` (renk/ölçü/tip), `src/AeroDark.cs` (koyu palet), `AeroEnv` (erişilebilirlik anahtarları), `Draw` (cam/gloss) |

> `ui-ux-pro-max` taramaları: `--domain ux` (focus appearance, active/hover feedback,
> durum geri bildirimi, compact control semantics) uygulandı. `--domain typography`
> sonuçları (Inter, Plus Jakarta, 16px gövde) **kullanılmadı**: Aero tarifi Segoe UI
> 12/16 gövde, 15/600 başlık, 11px ikincil ve 12/400 düğmeyi tartışmasız sabitliyor.
> `--design-system` çalıştırılmadı (Aero veritabanında yok). Stack kataloğunda
> WinForms bulunmadığı için `--stack` çıktısı (avalonia/wpf) yalnızca “klavye
> erişilebilirliği” ilkesi için okundu; kod GDI+ ile yazıldı.

## 2. Tartışmasız görsel hedef

- Cam **yalnız**: pencere çerçevesi + başlık çubuğu, modal, toast. Kartlarda/panellerde cam yok.
- Aynı anda **en fazla 2 cam yüzey** (pencere + modal **veya** pencere + toast); 3 sınırının altında.
- Ölçüler: titlebar 30 · düğme 23 · alan 22 · satır 24 · durum çubuğu 22 · cam padding 4.
- Köşe: pencere 8 · kontrol 3 · alan 2 · satır 0 · toast/modal 8/6.
- Tip: Segoe UI 9pt = 12px gövde, 8.25pt = 11px ikincil, 11.25pt = 15px/600 başlık,
  düğme 12/400, konsol Consolas 12px. Inter/Roboto/emoji yok.
- Gloss düğme yüzü, 4 duraklı yeşil progress, mavi seçim gradyanı, Explorer nav pane.
- 44px dokunma hedefi, 12px+ köşe, Mica/Fluent yuvarlaklığı, mor gradyan, blur-3xl yok.
- Token listesi dışında renk yok; yalnızca verilen tokenların karışımları
  (`Draw.Mix`) kenar/gölge türevleri için kullanılır.

## 3. Katmanlar

```
primitive (ham değer)             semantic (amaç)                 component (bileşen)
#2F7BBD                    →      --color-primary          →      --button-border-default
#FDFEFE…#EEF3F7 gradyan    →      --button-face            →      AeroButton yüzeyi
rgba cam durakları         →      --glass-tint / sheen     →      MainForm + AeroDialog + AeroToast
23px / 3px / 12px          →      --button-height/radius   →      AeroButton
```

CSS ada eşlemesi (tam liste `aero-tokens.css`):

| Amaç | CSS | C# |
|---|---|---|
| Gövde metni | `--color-foreground` | `Aero.Ink` |
| İkincil metin | `--color-muted-foreground` | `Aero.InkSoft` |
| Yüzey | `--color-surface` | `Aero.Paper` |
| Zemin | `--color-background` | `Aero.Chrome` |
| Birincil | `--color-primary` | `Aero.Blue600` |
| Cam tint/sheen | `--glass-tint`, `--glass-sheen` | `Aero.GlassTint*`, `Aero.GlassSheen*` dizileri |
| Düğme yüzü | `--button-face*` | `AeroButton.OnPaint` durakları |
| Progress | `progress.fill` | `AeroProgress` + `ToneStops` |
| Satır/seçim | `table.*` | `AeroGridView` |

## 4. Bileşen eşlemesi

| Uygulama | Aero karşılığı | Dosya |
|---|---|---|
| Ana pencere, kabuk | `.aero-window`: 4px cam padding, cam çerçeve, 30px titlebar | `src/MainForm.cs` |
| Pencere düğmeleri | 3 caption 26×17, kapat hover kırmızı, diğerleri mavi | `AeroTitleBar` |
| Sol gezinme | Explorer nav pane: opak `#F1F5F9`, 24px satır, mavi seçim | `AeroNav` |
| Üst araç çubuğu | Komut çubuğu: `#F6F9FC→#E6EDF4`, alt 1px `#CBD6E0`, birincil `aero-btn--default` | `AeroCommandBar` |
| Liste/tablo | Ayrıntı görünümü: 22px başlık, 24px satır, hover/seçim gradyanı, 1px ayırıcı | `AeroGridView` |
| Panel/kart | Group box: beyaz, 1px `#C9D4DE`, 3px, 10px padding, 12/600 başlık, **cam yok** | `AeroCard` |
| Modal | Cam çerçeve + 30px titlebar, gövde `#F1F5F9`, sağa yaslı buton şeridi | `AeroDialog` |
| Form alanı/arama | 22px, 2px köşe, iç gölge, odak `#4A9BDC` + 2px halka | `AeroSearchBox` |
| Onay kutusu | 13px, 3px köşe, mavi gradyan + beyaz tik | `AeroCheck` |
| Durum/ilerleme | 14px, 4 duraklı yeşil gradyan, üst yarıda sheen; işlem sırasında durum çubuğunda | `AeroProgress`, `AeroStatusBar` |
| Konsol | Group box içinde beyaz, Consolas 12px, **300 satır halka buffer**, yalnız yazı rengi | `AeroConsole` |
| Toast | Cam `.aero-float`, 6px köşe, sağ üst 6px, 4s, hover’da durur | `AeroToast` |
| Alt durum çubuğu | 11px `#3D556B`, `#EEF3F8→#E2E9F1`, üst 1px `#C9D4DE` | `AeroStatusBar` |
| Taskbar şeridi | Uygulamada taskbar yok → eklenmedi | — |

### Cam kullanılan yerler (tam liste)

1. `MainForm.OnPaintBackground` — pencere çerçevesi + 30px titlebar bandı
   (gradyan bandın kendi yüksekliğine sıkıştırılır, aksi halde cam beyaz görünür).
2. `AeroDialog.OnPaintBackground` — modal çerçeve + titlebar.
3. `AeroToast.OnPaintBackground` — bildirim.

Kartlar, nav pane, komut çubuğu, tablolar, form alanları ve durum çubuğu **opaktır**.

### Marka

`assets/logo.png` tek kaynak; `tools/make_icon.py` 16/20/24/32/40/48/64/128/256px `app.ico`
ve gömülü 64px `logo64.png` üretir. Ana titlebar ve modal titlebar'da 16px, ayrıca EXE/taskbar
simgesi. Logo yeniden renklendirilmez veya kırpılmaz; kare tuvale %96 oranla ortalanır
(`src/AeroLogo.cs`).

## 4b. Dil ve tema

- **Kaynak dil İngilizce.** Tüm UI metinleri `L.T("English key")` / `L.F("… {0}", …)` ile yazılır;
  `src/Lang.cs` içindeki `Tr` sözlüğü yalnız `L.Lang == "tr"` iken devreye girer. Eksik anahtar
  İngilizce'ye düşer; `L.Count`, `L.Percent`, `L.Culture` (en-US/tr-TR) sayı/tarih biçimini taşır.
- **Ayarlar sayfası** (`src/pages/SettingsPage.cs`): Dil (English / Türkçe) ve Görünüm
  (Light / Dark) kartları; seçili düğme `AeroButton.Selected` ile mavi gloss alır.
  Değişiklik `MainForm.ApplyPreferences(4)` ile anında tüm ağacı yeniden kurar.
- **Kalıcılık** (`src/AppSettings.cs`): `[ui] language=en|tr`, `theme=light|dark`.
  Önce EXE yanındaki `MineManager.ini`; yazılamıyorsa `%APPDATA%\MineManager\settings.ini`.
  Dosya yoksa varsayılan **English + Light**.
- **Koyu palet** (`src/AeroDark.cs`): açık palet static alanları bir kez yakalanır, `SetDark(false)`
  birebir geri yükler. `semantic.dark.*` tokenları `tokens.json` içinde; CSS handoff
  `[data-theme="dark"]` bloğu `aero-tokens.css` sonunda.
- **Native kaydırma çubukları:** DataGridView/console çocuk çubukları `src/DarkMode.cs` ile
  `DarkMode_Explorer` temasına bağlanır; Donanım panelinin non-client çubuğu gizlenip
  `src/AeroScrollBar.cs` ile çizilir (AutoScroll korunur).
- Yeni renk yok: koyu değerler de aynı tokenların koyu varyantları; metin türevleri
  `WarningText` / `SuccessText` / `DangerText` yine `Draw.Mix` ile hesaplanır.

## 5. Durum matrisi

| Bileşen | default | hover | active/pressed | focus-visible | disabled | selected |
|---|---|---|---|---|---|---|
| Düğme | gloss 4 durak + 1px kenar | mavi gloss, 150ms tween (reduced-motion’da anında) | iç gölge, metin +1px, ölçek yok | 2px `rgba(88,168,238,.9)` + koyu kontur | düz `#F2F5F8`, `#93A3B1` metin | — |
| Birincil düğme | gloss + çift mavi çerçeve + dış parıltı | aynı + mavi kenar | iç gölge | aynı | parıltı kapalı | — |
| Alan | beyaz + iç gölge + 1px | — | — | `#4A9BDC` kenar + 2px `rgba(120,190,250,.4)` | — | — |
| Onay kutusu | beyaz 13px kutu | mavi kenar | — | 2px halka | gri kutu/metin | mavi gradyan + beyaz tik |
| Tablo satırı | beyaz | `#F4FBFF→#E7F4FE` | — | hücrede 2px halka | — | `#D9EDFF→#BFE0F9` + `#86BFE7` |
| Başlık | `#FBFDFF→#EAF0F6` | — | — | — | — | — |
| Nav öğesi | opak zemin | açık mavi | — | 2px halka | — | mavi gradyan + `#86BFE7` + 600 |
| Caption | yarı saydam beyaz | mavi gloss (kapat: kırmızı) | koyu gloss | — | — | — |
| Toast | cam | yaşam süresi durur | tıklayınca kapanır | — | — | — |

## 6. Erişilebilirlik

Ölçülen kontrastlar (WCAG 2.x, sRGB):

| Çift | Oran |
|---|---|
| ink / beyaz | **15.01:1** |
| ink / chrome | **13.70:1** |
| ink / tablo başlığı alt | **13.08:1** |
| ink / hover satırı | **13.42:1** |
| ink / seçili satır | **10.90:1** |
| ink / cam opak fallback (orta) | **9.91:1** |
| ink-soft / beyaz | **7.75:1** |
| ink-soft / chrome | **7.07:1** |
| ink-soft / başlık | **6.75:1** |
| ink-soft / seçili satır | **5.63:1** |
| beyaz / blue700 | **7.10:1** |
| beyaz / danger (kapat hover) | **5.44:1** |
| `warning-text` #8A6C3C / beyaz | **4.89:1** |
| `success-text` #4A803B / beyaz | **4.73:1** |
| koyu ink #E8F1F9 / paper #202B38 | **12.55:1** |
| koyu ink / chrome #18212C | **14.22:1** |
| koyu ink-soft #A9BDD0 / paper | **7.43:1** |
| koyu ink / seçili satır #1A5F93 | **5.93:1** |
| koyu `warning-text` #F1CB8F / paper | **9.34:1** |
| koyu `success-text` #A7D88E / paper | **8.78:1** |
| koyu `danger-text` #D9857C / paper | **5.19:1** |
| disabled / beyaz | 2.59:1 — pasif kontrol, WCAG muaf |

- Gövde metni hiçbir yüzeyde 4.5:1 altına inmiyor. 11px `ink-soft` yalnız ikincil bilgi.
- Ham `warning` #E8A33D ve `success` #6CBB3C **dolgu/ikon** içindir; küçük metinde
  kontrastları 2.16:1 / 2.38:1 kalır. Bu yüzden metin için aynı tokenların ink ile
  karışımı (`WarningText`, `SuccessText`) kullanılır — yeni renk üretmeden türetme.
  Uyarı üçgenindeki “!” beyazdan ink'e alındı (amber üstünde 6.8:1).
- Odak halkası: 2px `rgba(88,168,238,.9)` + 1px `#1D5B91` kontur; açık yüzeylerde
  3:1 eşiğini geçer (yalnız halka 2.4:1 kalıyordu, kontur eklendi).
- Her etkileşimli öğe `TabStop=true`, Enter/Space ile çalışır (düğme, onay kutusu),
  nav ok tuşları/Home/End, tablo ok tuşları + Space, konsol PageUp/PageDown/Home/End.
- `AeroEnv.HighContrast` (Windows yüksek karşıtlık = forced-colors): cam → `Window`,
  metin → `WindowText`, seçim → `Highlight/HighlightText`, çerçeve → `WindowFrame`,
  gloss/parıltı kapalı. `AeroGridView`, nav, düğme, onay kutusu, komut çubuğu, durum
  çubuğu, konsol, modal ve toast bu yolu izler.
- `AeroEnv.ReducedTransparency` (`EnableTransparency=0` veya yüksek karşıtlık):
  cam → opak `#E9F5FF→#B4D6F4→#C4E2FA`, sheen kapalı.
- `AeroEnv.ReducedMotion` (`SPI_GETCLIENTAREAANIMATION=0` veya yüksek karşıtlık):
  düğme hover tween’i atlanır, toast fade’i atlanır; başka animasyon yok.
- `prefers-reduced-transparency`, `prefers-contrast: more`, `prefers-reduced-motion`,
  `forced-colors: active` karşılıkları `aero-tokens.css` sonunda yazılı.
- `backdrop-filter`/blur katmanı yok (GDI+ blur yapmıyor); `will-change` yok;
  cam yalnız 2 yüzeyde, animasyon sırasında blur değişmiyor.

## 7. Ölçü ve daralma

| Genişlik | Davranış |
|---|---|
| ≥ 900 | nav 190px, RAM sayfası iki sütun |
| 620–899 | nav 190px, RAM iki sütun (≥760) |
| < 760 | RAM sayfası dikey yığılır (gauge + konsol üstte, işlemler altta) |
| < 620 | nav 46px ikon moduna daralır (tooltip’li) |
| < ~534 (tablo) | sütunlar `MinimumWidth` korur, tablo yatay kaydırılır |
| dar komut çubuğu | taşan eylemler “Diğer” menüsüne alınır (aynı `PerformClick`) |

Pencere minimumu **320×560**; 320–1440 aralığı doğrulandı (320/360/720/1100/1440).

## 8. Bilinçli olarak yapılmayanlar

- **Sıralama oku yok**: uygulama sütunları hiçbir zaman sıralanabilir değildi;
  sıralama eklemek davranış değişikliği olurdu.
- **Tabs/Select primitifi yok**: uygulamada sekme/select yok; sol nav pane sekmelerin
  yerini alıyor. Davranış (5 sayfa: Temizlik, RAM, Kaldırıcı, Donanım, Ayarlar; aynı `ShowPage`) korunuyor.
- **Taskbar şeridi yok**: uygulamanın kendi taskbar’ı yok; uydurulmadı.
- Sistem `SaveFileDialog` ve sistem menüsü OS tarafından çizilir; native çocuk kaydırma
  çubukları koyu temada `DarkMode.cs`, Donanım paneli `AeroScrollBar` ile boyanır.
- Ölümcül başlangıç hataları sistem `MessageBox`'ında gösterilir; tema/GDI+ yüklenemese
  bile hata görünür kalmalı. Tek örnek uyarısı Aero modalına taşındı.
- Gerçek DWM bulanıklığı Win8+’ta kaldırıldığı için cam, tarifteki tint + sheen +
  1px beyaz iç çizgi + rim ile boyanıyor; opak fallback aynı görünümü korur.

## 9. Teslim kontrol listesi

- [x] Cam yüzey sayısı ≤ 3 (gerçekte 2: pencere + modal **veya** toast)
- [x] Metin kontrastı ≥ 4.5:1 (pasif kontrol hariç; tablo §6)
- [x] hover / active / focus / disabled / selected her bileşende tanımlı
- [x] 320–1440px arasında kırılma yok; tablolar yatay kaydırma, RAM dikey yığılma,
      komut çubuğu “Diğer” menüsü
- [x] reduced-motion’da geçiş yok (hover tween + toast fade kapalı)
- [x] reduced-transparency’de cam opak, sheen kapalı
- [x] high-contrast/forced-colors sistem renkleri
- [x] Klavye ile tüm eylemler (düğme, onay kutusu, nav, tablo, konsol, ESC=İptal)
- [x] Tek EXE derlemesi: `dist/MineManager.exe` ~328 KB (logo ICO dahil)
- [x] Dil: İngilizce varsayılan, Türkçe eksiksiz sözlük; `MineManager.ini` ile kalıcı
- [x] Koyu tema: tüm sayfalar, modal, toast ve kaydırma çubukları; açığa dönüş kayıpsız
- [x] Ekran görüntüleri: `design-system/minemanager/screens/` (18)

## 10. Ekranlar

| Dosya | Ekran |
|---|---|
| `screens/01-cleaner.png` | Kabuk + Temizlik, EN açık (1100×720) |
| `screens/02-memory.png` | RAM + 300 satır konsol |
| `screens/03-uninstaller.png` | Kaldırıcı + boş kalıntı durumu |
| `screens/04-hardware.png` | Donanım group box'ları + özel scrollbar |
| `screens/05-settings.png` | Ayarlar: dil + görünüm |
| `screens/06-dialog-dark.png` | Koyu Aero modal (cam çerçeve, logo) |
| `screens/07-toast-dark.png` | Koyu cam bildirim |
| `screens/08-cleaner-tr-dark.png` | Temizlik, TR koyu |
| `screens/09-memory-tr-dark.png` | RAM, TR koyu |
| `screens/10-uninstaller-tr-dark.png` | Kaldırıcı, TR koyu |
| `screens/11-hardware-tr-dark.png` | Donanım, TR koyu (özel scrollbar) |
| `screens/12-settings-tr-dark.png` | Ayarlar, TR koyu |
| `screens/13-320-cleaner.png` | 320px: komut çubuğu + yatay tablo |
| `screens/14-320-memory.png` | 320px: dikey RAM + taşma menüsü |
| `screens/15-320-uninstaller.png` | 320px: arama + iki tablo |
| `screens/16-320-hardware.png` | 320px: etiket/değer alt alta |
| `screens/17-320-settings.png` | 320px: ayar kartları + tam not |
| `screens/18-restart-tr-dark.png` | INI'den açılış: TR + koyu korunuyor |
