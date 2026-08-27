# INTSHOP Frontend — SvelteKit (deneme)

> Bu klasör `sveltekit_gecis` branch'inde plain Svelte + Vite SPA'dan
> SvelteKit'e geçiş denemesi olarak yeniden yapılandırıldı. `main`
> branch'inde hâlâ eski Vite + Svelte 5 yapısı geçerli.

## Çalıştırma

Kök dizinden `docker compose up` yeterli; bu servis `npm run dev` ile
`http://localhost:5173` üzerinde ayağa kalkar (komut aynı, SvelteKit de
Vite üzerinde çalışıyor).

## Yapı

Dosya tabanlı routing kullanılıyor (`src/routes/`):

```
src/routes/
├── +layout.svelte        # Oturum kontrolü, toolbar, role bazlı guard
├── +page.svelte          # Kök yol — role göre yönlendirme
├── +error.svelte         # Genel hata sayfası
├── magaza/                # Müşteri: ürün listesi + [id] ürün detayı
├── sepet/                 # Müşteri: sepet
├── bayi/                  # Bayi: stok, raporlar, talepler
└── admin/                 # Admin: bayiler, ürünler, kategoriler, istekler, kullanıcılar, hareketler, loglar
```

Sayfa bileşenlerinin çoğu (`src/lib/Components`, `src/lib/Modals`) eski
yapıdan olduğu gibi taşındı; route dosyaları çoğunlukla bunları saran ince
sarmalayıcılar.

## Bilinçli kısayollar (PoC kapsamı)

- **SSR kapalı** (`+layout.js` → `ssr = false`). Oturum bilgisi
  `localStorage`'a bağlı olduğu için sunucu tarafında çökerdi; gerçek bir
  migration'da bu, oturumun server-side okunabilir hale getirilmesini
  gerektirir.
- **Route guard client-side.** `+layout.svelte` içinde, giriş yapan
  kullanıcının rolüne uygun olmayan bir yola girilirse (`goto` ile) kendi
  ana sayfasına geri yönlendirilir ve o sayfa hiç render edilmez. Backend
  zaten her endpoint'te ayrıca rol kontrolü yapıyor; bu yalnızca istemci
  tarafında da doğru sayfayı göstermek için.
