# stok-panel (KOBURA) — Kod Tabanı Analizi ve İyileştirme Önerileri

## Özet

**stok-panel**, üç rollü (Admin / Bayi / Kullanıcı) bir stok ve satış paneli uygulamasıdır. Admin ürünleri ve kullanıcıları yönetir; Bayi kendi stok giriş/çıkış hareketlerini yapar, fiyatlarını referans fiyatın belirli bir yüzde bandı içinde günceller; Kullanıcı (müşteri) bayilerin sattığı ürünleri bir mağaza görünümünde gezer. Backend, ASP.NET Core 8 + Dapper + PostgreSQL; frontend, Svelte 5 (runes) + Vite + Chart.js. Docker Compose ile üç servis (db, api, frontend) ayağa kalkar.

> **Kritik not:** `db/init/01-seed.sql` şeması kodla senkron değil. Kodun kullandığı `users.email`, `users.avatar_url`, `products.min_oran`, `products.max_oran` ve `dealer_stock.price` sütunları seed dosyasında **yok**. Bu haliyle login/signup/my-stock/shop uçları `column does not exist` hatası verir; projenin şu an çalışıyor olması ancak veritabanının elle `ALTER TABLE` ile genişletilmiş olmasıyla mümkündür (aşağıda detay).

---

## Mimari

- **Katman:** Tek katmanlı, controller'lar doğrudan Dapper ile SQL çalıştırır. Service/repository/DTO katmanı yok.
- **Auth modeli:** ASP.NET kimlik doğrulama altyapısı yok. Her login'de rastgele bir token üretilir, `users.token` sütununa yazılır; istemci bu token'ı `Authorization` başlığında (Bearer öneki olmadan) gönderir. Her korumalı endpoint, her istekte `users` tablosunu token'a göre sorgulayıp rolü kendisi kontrol eder (`AuthHelper.GetRole` veya controller içindeki kopya `GetUser`).
- **Veri erişimi:** `IDbConnection` scoped olarak `Program.cs`'te NpgsqlConnection ile kayıtlı; Dapper extension'larla açılıp kullanılıyor. Hiçbir yerde transaction yok.
- **Veritabanı şeması:** Migration aracı yok; şema yalnızca `db/init/01-seed.sql` (ilk boot'ta çalışır) ile yönetiliyor.
- **Akış başlangıcı:**
  1. `docker-compose up` → `db` (postgres:18) init SQL'i çalıştırır, `api` (ASP.NET Core) `8080`'e, `frontend` (Vite dev server) `5173`'e bağlanır.
  2. Frontend `App.svelte` mount olur; localStorage'daki token/role'a göre hangi oturumun açılacağına karar verir.
  3. İstekler `http://localhost:5081/api/...` adresine gider (docker'da API_PORT=5081 → container 8080).

### Teknoloji yığını

| Katman | Teknoloji |
|---|---|
| Backend | .NET 8, ASP.NET Core minimal hosting, Dapper 2.1.79, Npgsql 10.0.3, BCrypt.Net-Next 4.2.0, Swashbuckle (kullanılmıyor) |
| Frontend | Svelte 5 (runes), Vite 8, Chart.js 4.5.1 |
| Veritabanı | PostgreSQL 18 (Docker) |
| Orkestrasyon | docker-compose (db/api/frontend) |

---

## Dizin Yapısı

```
stok-panel/
├── .env.example              — POSTGRES_USER/PASSWORD/DB, API_PORT (boş bırakılmış!)
├── docker-compose.yaml       — db (postgres:18) + api + frontend (Vite dev)
├── db/init/01-seed.sql       — Şema + demo verisi (kodla uyumsuz, kritik!)
├── backend/
│   ├── Program.cs            — Minimal host: CORS, IDbConnection DI, controller map
│   ├── AuthHelper.cs         — Token → rol sorgusu (statik, kopyalanmış mantık)
│   ├── StokApi.csproj
│   ├── StokApi.http          — Şablon kalıntısı (weatherforecast uç olmayan endpoint)
│   ├── Properties/launchSettings.json — localhost:5077 (frontend 5081 bekliyor!)
│   └── Controllers/
│       ├── AuthController.cs      — register (Admin-only), login, signup
│       ├── ProductsController.cs  — Ürün CRUD + yeni ürüne her bayiye 0 stok satırı
│       ├── StockController.cs     — Bayi: my-stock, movement, movements, history, price
│       ├── ShopController.cs      — Kullanıcı: satılan ürünler (offset/limit)
│       ├── ProfileController.cs   — profil get/put
│       ├── UsersController.cs     — Admin: dealers/users listesi
│       ├── MovementsController.cs — Admin: tüm hareketler
│       └── CategoriesController.cs— kategori listesi
└── frontend/
    ├── src/
    │   ├── App.svelte         — TÜM uygulama tek dosyada (~1100 satır)
    │   ├── app.css            — Tüm stiller tek dosyada
    │   ├── main.js            — mount noktası
    │   └── lib/Counter.svelte — Vite şablon kalıntısı (kullanılmıyor)
    ├── public/products/, public/avatars/ — statik resimler
    └── package.json, vite.config.js (polling: Docker volume için)
```

---

## Kritik Bulgular (Kod ile DB Şeması Uyumsuzluğu)

`01-seed.sql`'de tanımlı tablolar ile controller'ların sorguladığı sütunlar eşleşmiyor:

| Kodun kullandığı | Seed'de durum | Etkilenen uç |
|---|---|---|
| `users.email` | ❌ yok | `AuthController.Signup` (INSERT), `Register` (INSERT) |
| `users.avatar_url` | ❌ yok | `AuthController.Login` (SELECT), `ProfileController` |
| `products.min_oran`, `max_oran` | ❌ yok | `StockController.MyStock`, `UpdatePrice` |
| `dealer_stock.price` | ❌ yok (yalnız stock var) | `StockController.MyStock`, `UpdatePrice`, `ShopController.Shop` |

**Çözüm önerisi:** Migration aracına geçin (EF Core Migrations veya FluentMigrator) ya da en azından seed dosyasını kodla birebir senkron tutun. Bu, projeye yeni başlayan birinin yaşayacağı ilk ve en yıkıcı hatadır.

---

## Backend: Sorunlar ve Öneriler

### 1. Güvenlik (en kritik)

- **Manuel token doğrulaması:** 8 controller'dan 6'sı `GetUser`/`GetRole` kodunu kopyalamış. `[Authorize]` yok, authentication middleware yok. Token'ın süresi/iptali yok; token DB'de düz metin saklanıyor.
- **Öneri:** ASP.NET Core JWT Bearer + `[Authorize(Roles = "Admin")]` kullanın. Token'ı DB'de SHA-256 hash'li tutun, expiry ekleyin. Manuel kontrolü bir `AuthService` (scoped) + ActionFilter'a indirgeyin.
- **Register yetki yükseltme:** Admin, `req.role` ile **"Admin" dahil** herhangi bir rol oluşturabiliyor. Rolü sunucuda whitelist ile sınırlayın (`"Kullanici"` / `"Bayi"`); Admin oluşturmayı ayrı bir akışa alın.
- **Register hata yönetimi:** `catch { return BadRequest("Bu kullanıcı adı zaten var"); }` — her istisnayı (DB çökmesi dahil) "kullanıcı adı var" diye yutturuyor; ayrıca email çakışmasını da yanlış mesajla raporluyor. `Signup`'taki `PostgresException 23505` yakalama desenini `Register`'a da uygulayın. Register'da şifre politikası yok (Signup'ta var) — tutarlı hale getirin.
- **Yanıt formatı:** Düz string + `StatusCode(403, ...)` yerine `ProblemDetails` kullanın.
- CORS yalnızca `localhost:5173`'e açık — production'da yapılandırılabilir olmalı.

### 2. Veri tutarlılığı / Race condition

`StockController.AddMovement` üç ayrı sorgu yapıyor (mevcut stoku oku → hareket INSERT → stok UPDATE) **transaction yok**. İki eşzamanlı istek stoku eksiye düşürebilir.

**Öneri:** Tek transaction içinde `SELECT ... FOR UPDATE` (veya `UPDATE dealer_stock SET stock = stock + @change WHERE ... AND stock + @change >= 0` gibi koşullu güncelleme) + hareket kaydı. Dapper'da `IDbTransaction` ile.

### 3. Ölü / gereksiz kod

- `ProductsController.cs` içinde **iki** `NewProduct` record'u var: dosya seviyesinde (`stock` içeren) ve class içinde (`stock` içermeyen). Class içindeki kazanıyor; üstteki tamamen ölü kod. Kaldırın.
- `StockController` → `using System.Reflection.Metadata.Ecma335;` kullanılmıyor.
- `ShopController` → `using System.Runtime.InteropServices;` kullanılmıyor.
- `StokApi.http` → var olmayan `/weatherforecast` çağırıyor; silin veya gerçek uçlarla değiştirin.
- `StokApi.csproj` → `Swashbuckle.AspNetCore` paketi referanslı ama `Program.cs`'te Swagger hiç kurulmamış (`launchSettings` ise `launchUrl: swagger` diyor). Ya `AddSwaggerGen`/`UseSwagger` ekleyin ya da paketi kaldırın.
- `launchSettings.json` → `http://localhost:5077`; frontend 5081'e istek atıyor; Docker 8080→5081 map'liyor. Üç ayrı port iddiası var; tek bir env değişkenine (`API_PORT`) bağlayın.

### 4. Diğer backend sorunları

- `AuthHelper.GetRole`'daki SQL'de `Where` büyük harf — çalışır ama tutarsız stil.
- Role string'leri ("Admin", "Bayi", "Kullanici") 6 controller'da sihirli string olarak dağılmış — sabit/enum kullanın.
- `Shop` ucu `limit`/`offset` doğrulamıyor (negatif limit, dev limit).
- `AddMovement` `change = 0` veya boş hareketi engellemiyor.
- `ProductsController.Update` stok güncellemiyor; `UpdateProduct` record'unda `stock` yok — UI'de de stok alanı yok. `products.stock` sütunu pratikte hep 0 kalıyor → admin tablosundaki tüm satırlar "dusuk" (kırmızı) görünüyor. Ya stoku modele ekleyin ya da sütunu tamamen kaldırıp yalnızca `dealer_stock`'a güvenin.

---

## Frontend: Sorunlar ve Öneriler

`App.svelte` **tüm uygulamayı** (auth, dashboard, 6 tablo, 2 modal, mağaza, grafik, profil) tek dosyada barındırıyor (~1100 satır). Bu, şu anki kötü kokuların ana kaynağı.

### 1. Gerçek buglar

- **`kullaniciEkle` vs `kullaniciekle`:** Script'te fonksiyon `kullaniciekle` olarak tanımlı; şablondaki "Kullanıcı Ekle" modal butonu `onclick={kullaniciEkle}` çağırıyor. İsim uyuşmuyor — Svelte derleme hatası/bozuk buton. Biri mutlaka düzeltilmeli.
- **`shopOffset = $state([])`:** Dizi olarak başlatılmış; `shopOffset += yeni.length` → diziyi sayıya ekleyince **string birleştirme** olur ("14", sonra "1414"…). Sonsuz sayfalama döngüsü kırılır. `shopOffset = $state(0)` olmalı.
- **`kayitOl` catch bloğunda `string(e)`:** `string` tanımsız bir fonksiyon → signup hatasında `ReferenceError` fırlar, kullanıcıya mesaj gösterilmez. `String(e)` olmalı (dosyadaki diğer tüm catch'ler doğru `String(e)` kullanıyor — bu tek satır yanlış).
- **`logout()`:** `location.reload()` sonrası `shopSifirla()` çağrılıyor — reload sayfayı öldürdüğü için bu satır hiç çalışmaz. Ayrıca state sıfırlama + reload birlikte gereksiz; ikisinden birini seçin.
- **`duzenlenenId = $state(false)`:** `false` ile başlıyor, sonra `null` veya sayı alıyor; tip tutarsız. `$state(null)` kullanın.
- **`Sepete Ekle` butonu işlevsiz** — ne onclick ne handler var.

### 2. Ölü kod

- `addProduct()`, `name`, `categoryId`, `stock`, `price` state'leri hiçbir yerden çağrılmıyor (ürün ekleme artık modal üzerinden `urunKaydet` ile yapılıyor). Tümü silinebilir.
- `aktifSekme === "fiyatlandirma"` toolbar'da Admin ve Bayi'ye gösteriliyor ama içerikte **hiçbir blok yok** — tıklayınca boş sayfa. Ya içerik ekleyin ya butonu kaldırın.
- `indirimAyar` bloğu var ama toolbar `indirim` kullanıyor ("İndirimler" sekmesi boş `<h2>`). Aynı şekilde ya doldurun ya kaldırın.
- `src/lib/Counter.svelte`, `src/assets/svelte.svg`, `vite.svg` — Vite şablon kalıntıları.

### 3. Kod tekrarı — refactor önerileri

- Her fonksiyonda `fetch(...)` + `if(!res.ok) throw` + catch deseni ~15 kez tekrarlanıyor. **`src/lib/api.js`** modülü yazın: `api.get(path)`, `api.post(path, body)`; 401'de otomatik logout. Hata gösterimi de (alert vs error state) tutarlı olsun.
- `sayfala`/`toplamSayfa`/`sayfaGit` + pagination DOM'u 5 kez kopyalanmış → tek bir `DataTable.svelte` bileşeni.
- Resim URL normalizasyonu (`onizlemeYolu` ile `urunKaydet` içindeki mantık aynı) tek yardımcı fonksiyona alınmalı.
- `loadShop` içindeki `console.log` debug ifadeleri kalmış — silin.
- `loadHistory` ve `raporlar` effect'i `setTimeout(drawChart, 0)` ile grafiği çiziyor. `tick()` + `$effect` veya `bind:this` ile temiz çözün; `onDestroy`'de `chartInstance.destroy()` çağırın.

### 4. CSS sorunları

- **`sepet-btn:active`** — başında nokta yok; geçersiz seçici, hiç çalışmaz.
- `<div style="display: flex-wrap">` — geçersiz; `display: flex; flex-wrap: wrap` olmalı ("raporlar" bölümünde iki kez).
- `.sepet-btn` içinde `align-items: right` — geçersiz değer; `justify-content: flex-end` olmalı.
- `.kayit-baslik`'a `perspective` verilmiş — anlamsız; perspektif zaten `.flip-cerceve`'de.
- `.modal .modal-input, .modal select` kuralı — `modal-input` sınıfı hiçbir elementte yok (ölü kural).
- Tablo kenarlıkları `whitesmoke` (neredeyse beyaz) — açık zeminde görünmez; şu an tüm tablolar "kenarlıksız" görünüyor.
- `.toolbar-user` kullanılmıyor.
- Renkler tekrar tekrar yazılıyor (`lightseagreen`, `whitesmoke` vb.) — CSS değişkenlerine alın.
- Login arka planı `repeating-radial-gradient(circle, blue, lightseagreen, lightblue, whitesmoke)` — göz yoran bir seçim; sabit bir degrade/tema önerilir (subjektif ama marka tutarlılığı için).

### 5. Diğer frontend notları

- `index.html`: `lang="en"`, `<title>app</title>` — Türkçe arayüz için `lang="tr"` + anlamlı başlık.
- Chart.js tüm `registerables` ile import ediliyor — ağırlık istemiyorsanız yalnızca bar elementi için minimal import.
- `profil.username` input'una `bind:value` yok ve `disabled` da değil — kullanıcı yazamaz ama alan etkin görünür; `readonly` yapın.
- `loading` başlangıçta `true`; login olmadan önce de "Yükleniyor…" metni görünür — başlangıç değerini rol varlığına göre ayarlayın.

---

## Veri Akışı

1. **Giriş:** `App.svelte` → `POST /api/login` → `AuthController.Login` → BCrypt.Verify → token üretip `users.token`'a yazar → `{token, role, username, avatar_url}` döner → localStorage'a kaydedilir.
2. **Sonraki istekler:** `Authorization: <token>` başlığı (Bearer önekli değil) → controller kendi içinde `SELECT role FROM users WHERE token = @token` çalıştırır.
3. **Bayi stok:** `GET /api/my-stock` → `dealer_stock` ⋈ `products` ⋈ `categories`; referans fiyat, bayi fiyatı (`ds.price`), alt/üst sınır (`min_oran`/`max_oran` — şemada eksik!) döner.
4. **Bayi hareket:** `POST /api/my-stock/movement` → mevcut stok okunur → `stock_movements` INSERT → `dealer_stock` UPDATE (transaction yok, race mümkün).
5. **Bayi fiyat:** `PUT /api/my-stock/price` → referans fiyatın `min_oran`–`max_oran` bandı kontrol edilir → `dealer_stock.price` güncellenir.
6. **Admin ürün ekleme:** `POST /api/products` → ürün INSERT → ardından her `Bayi` için `dealer_stock`'a 0 stoklu satır (`NOT EXISTS` korumalı).
7. **Admin ürün silme:** sırayla `stock_movements`, `dealer_stock`, `products` DELETE — elle kademeli silme; FK ON DELETE CASCADE ile tek sorguya inerdi.
8. **Kullanıcı mağaza:** `GET /api/shop?offset&limit` → `ds.stock > 0 AND ds.price IS NOT NULL` → frontend `IntersectionObserver` ile sentinel'e gelince sonraki sayfayı çeker.
9. **Raporlar:** `GET /api/my-stock/history` günlere göre giriş/çıkış toplamı → Chart.js bar grafiği.

---

## Modül Referansı

| Dosya | Amaç |
|---|---|
| `backend/Program.cs` | Host kurulumu; CORS; scoped IDbConnection DI |
| `backend/AuthHelper.cs` | Token → rol sorgusu (statik; 3 controller'da kopyalanmış) |
| `backend/Controllers/AuthController.cs` | login / signup / register (Admin-only); BCrypt; token üretimi |
| `backend/Controllers/ProductsController.cs` | Ürün CRUD; ürün eklerken bayilere 0 stoklu satır açma |
| `backend/Controllers/StockController.cs` | Bayi stok görünümü, hareket ekleme, fiyat bandı kontrolü, geçmiş |
| `backend/Controllers/ShopController.cs` | Mağaza listesi (offset/limit, Kullanici-only) |
| `backend/Controllers/ProfileController.cs` | Profil get/güncelle (tüm roller) |
| `backend/Controllers/UsersController.cs` | Admin: bayi & kullanıcı listeleri |
| `backend/Controllers/MovementsController.cs` | Admin: tüm stok hareketleri |
| `backend/Controllers/CategoriesController.cs` | Kategori listesi |
| `db/init/01-seed.sql` | Şema + demo veri (kodla uyumsuz — kritik) |
| `docker-compose.yaml` | db+api+frontend orkestrasyonu |
| `frontend/src/App.svelte` | Tüm SPA (auth, paneller, tablolar, modallar, mağaza, grafik, profil) |
| `frontend/src/app.css` | Tüm stiller |
| `frontend/src/main.js` | Svelte mount |

---

## Önerilen Okuma / İyileştirme Sırası

1. `db/init/01-seed.sql` — Şemayı koda senkronlamadan hiçbir şey çalışmaz; önce bu.
2. `backend/Program.cs` + `AuthHelper.cs` — Auth mimarisinin temeli; JWT'ye geçiş burada başlar.
3. `backend/Controllers/AuthController.cs` — En çok güvenlik açığı içeren dosya.
4. `backend/Controllers/StockController.cs` — En karmaşık iş mantığı + transaction eksikliği.
5. `frontend/src/App.svelte` — Tüm frontend; bileşenlere bölme kararı buradan başlar.
6. `docker-compose.yaml` + `.env.example` — Ortam değişkenleri ve port tutarlılığı.

---

## Öncelikli Yapılacaklar (Özet)

**Acil (bozuk çalışma / güvenlik):**
- [ ] Seed şemasını kodla senkronla: `users.email`, `users.avatar_url`, `products.min_oran/max_oran`, `dealer_stock.price`
- [ ] `kullaniciEkle`/`kullaniciekle` isim uyuşmazlığı
- [ ] `shopOffset = []` → `0`
- [ ] `string(e)` → `String(e)`
- [ ] `AddMovement`'ta transaction + race koruması
- [ ] Register'da rol whitelist'i (Admin oluşturmayı kapat)
- [ ] JWT Bearer + `[Authorize(Roles)]`'e geçiş

**Önemli (kötü kod / bakım):**
- [ ] `App.svelte`'i bileşenlere böl (`api.js`, `DataTable`, `ProductModal`, `UserModal`, `ShopGrid`)
- [ ] Ölü kodu temizle: çift `NewProduct`, `addProduct`, `fiyatlandirma`/`indirim` içeriksiz sekmeler, `StokApi.http`, `Counter.svelte`, debug console.log'lar
- [ ] Swagger'ı ya kurun ya kaldırın; launchSettings portu 5081 ile senkronlayın
- [ ] Pagination'ı tek bileşene indirgeyin; CSS hatalarını düzeltin (`sepet-btn:active`, `display: flex-wrap`, `align-items: right`)
- [ ] Hata yönetimini `ProblemDetails` + tek tip frontend gösterimiyle standartlaştırın

---

Detaylı rapor `project_info__1.md` olarak proje köküne kaydedildi. İsterseniz belirli bir bölümde (örn. güvenlik veya `App.svelte` refactor'ü) daha da derinleşebilirim.