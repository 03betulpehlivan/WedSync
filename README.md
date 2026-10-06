<div align="center">
  
# 💒 KarWed | İnteraktif Düğün & Etkinlik Fotoğraf Paylaşım Platformu (SaaS)

[![.NET](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![SignalR](https://img.shields.io/badge/SignalR-0078D4?style=for-the-badge&logo=microsoft&logoColor=white)]()
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](https://opensource.org/licenses/MIT)

**Düğünlerde misafirlerin anlık olarak fotoğraf ve mesaj yüklemesine, bu anıların dev ekranda canlı (canlı slayt gösterisi) olarak yayınlanmasına olanak tanıyan yeni nesil etkinlik etkileşim platformudur.** 📸✨

</br>
</div>

---

## 🎯 Projenin Amacı (Nedir?)

KarWed klasik, sıkıcı düğün salonu yönetim sistemlerinden **farklıdır.** Bu proje, düğün sahipleri ve misafirleri bir araya getiren interaktif bir **SaaS (Hizmet Olarak Yazılım)** platformudur. 

**Nasıl Çalışır?**
1. **QR Kod ile Katılım:** Misafirler masalarındaki QR kodu okutarak uygulamaya bağlanır (Uygulama indirmeye gerek yoktur).
2. **Anlık Yükleme:** Kameralarından çektikleri fotoğrafları veya iyi dilek mesajlarını sisteme yüklerler.
3. **Canlı Slayt (Slideshow):** Yüklenen içerikler eşzamanlı olarak (SignalR ile) düğün salonundaki dev ekrana düşer.
4. **Moderasyon:** İstenmeyen mesajlar veya fotoğraflar admin panelinden saniyeler içinde onaylanabilir veya reddedilebilir.

## ✨ Temel Özellikler

- **⚡ Gerçek Zamanlı Slayt Gösterisi:** SignalR teknolojisi ile sayfayı yenilemeden anlık fotoğraf/mesaj düşmesi.
- **� Ziyaretçi Deneyimi (Guest Flow):** Misafirlerin kolayca fotoğraf ve mesaj atabileceği, hiçbir kurulum gerektirmeyen mobil uyumlu arayüz.
- **🛡️ İçerik Moderasyonu:** Admin panelinden gelen fotoğrafların ekrana yansımadan önce onaylanıp (Approve) reddedilebilmesi.
- **� B2B & White Label (Düğün Salonları İçin):** Düğün mekanlarının kendi logoları, isimleri ve marka renkleriyle (ThemeColor) sistemi müşterilerine sunabileceği "Salon Business" mimarisi.
- **� Çoklu Katmanlı Abonelik (SaaS Seçenekleri):** Free, Plus, Pro ve SalonBusiness gibi farklı abonelik katmanları.
- **💳 Online Ödeme Entegrasyonu:** (Ödeme altyapısı hazırlğı).

## 🚀 Teknolojik Altyapı

- **Backend:** C# & .NET Core MVC
- **Gerçek Zamanlı İletişim (WebSockets):** ASP.NET Core SignalR (`SlideshowHub`, `AdminHub`)
- **Veritabanı:** MS SQL Server / Entity Framework Core
- **Güvenlik & Yetkilendirme:** ASP.NET Core Identity (Google OAuth login desteği ile)
- **Frontend:** Modern Responsive UI (Razor Views, CSS3, JavaScript)

## 📂 Kod Mimarisi

- **`Models/WeddingEvent.cs`:** Çift bilgileri, etkinlik tarihi, salon ID'si, tema rengi ve B2B abonelik tiplerini tutan ana model.
- **`Models/GuestEntry.cs`:** Misafirlerden gelen fotoğraflar, mesajlar ve "onaylandı (IsApproved)" moderasyon bilgisini taşıyan model.
- **`Hubs/`:** Ekranların güncellenmesini sağlayan gerçek zamanlı SignalR hub'ları.
- **`Controllers/GuestController.cs`:** Misafirlerin dışarıdan sisteme veri yollamasını sağlayan endpoint'ler.
- **`Controllers/SlideshowController.cs`:** Salondaki dev yansıtma ekranı mantığını yöneten sınıf.

## 🛠️ Geliştirici Kurulumu

1. Projeyi klonlayın:
   ```bash
   git clone https://github.com/EnesKaraca44/KarWed.git
   ```
2. Bağımlılıkları yükleyin:
   ```bash
   dotnet restore
   ```
3. `appsettings.json` içerisinden veritabanı bağlantınızı (Connection String) ayarlayın.
4. Veritabanını oluşturun:
   ```bash
   dotnet ef database update
   ```
5. Projeyi başlatın:
   ```bash
   dotnet run
   ```

## 👨‍💻 Geliştirme ve İletişim

**Enes Karaca**
- GitHub: [@EnesKaraca44](https://github.com/EnesKaraca44)
