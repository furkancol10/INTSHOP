-- ============================================================
-- stok-panel — Geliştirme/demo seed verisi
--
-- BU DOSYA OTOMATİK ÇALIŞMAZ (docker-entrypoint-initdb.d dışında).
-- Yalnızca yerel/geliştirme ortamında, isteğe bağlı olarak elle uygulayın:
--
--   docker compose exec -T db psql -U <POSTGRES_USER> -d <POSTGRES_DB> < db/seed/dev-seed.sql
--
-- UYARI: Aşağıdaki hesapların şifreleri bilinen, zayıf demo şifreleridir
-- (admin123, bayi123, user123). Bu dosyayı ASLA internete açık veya
-- prod/staging bir veritabanına uygulamayın.
--
-- Demo kullanıcıları (BCrypt hash'li şifreler):
--   admin / admin123   (Admin)
--   bayi  / bayi123    (Bayi)
--   bayi2 / bayi123    (Bayi)
--   bayi3 / bayi123    (Bayi)
--   user1 / user123    (Kullanici)
--   user2 / user123    (Kullanici)
-- ============================================================

INSERT INTO users (username, password_hash, role, address, phone, avatar_url, email) VALUES
    ('admin', '$2a$11$/HEWwh0XFN2vBgwD7DsbFOoxQPtvtw5ChTTOi2c0i0uLReO8d4JH2', 'Admin',     NULL,                NULL,             NULL, 'admin@intshop.local'),
    ('bayi',  '$2a$11$4nMDaYWhemDQOc5K5.C4a.gJxn89kekAOjcTAfWz/MTt0wDb7hoz2', 'Bayi',      'Istanbul, Tuzla',   '0532 111 22 33', NULL, 'bayi@intshop.local'),
    ('bayi2', '$2a$11$4nMDaYWhemDQOc5K5.C4a.gJxn89kekAOjcTAfWz/MTt0wDb7hoz2', 'Bayi',      'Istanbul, Kartal',  '0532 111 44 55', NULL, 'bayi2@intshop.local'),
    ('bayi3', '$2a$11$nUeo6B4.gwLflsYYzOOo0evbBnv6C17FFJn6/9FcrZNDhBDVIhFN2', 'Bayi',      'Istanbul, Kadikoy', '0538 666 77 88', NULL, 'bayi3@intshop.local'),
    ('user1', '$2a$11$KM/OVZMbSJPZYNrU60ZSpuIKl1jQ15buBKlSgqI3yKifLwhuKf13q', 'Kullanici', 'Ankara, Cankaya',   '0533 222 33 44', NULL, 'user1@intshop.local'),
    ('user2', '$2a$11$KM/OVZMbSJPZYNrU60ZSpuIKl1jQ15buBKlSgqI3yKifLwhuKf13q', 'Kullanici', 'Izmir, Bornova',    '0534 555 66 77', NULL, 'user2@intshop.local');


-- Her bayiye her urunden stok + taban fiyatin %90-%110 arasinda satis fiyati
INSERT INTO dealer_stock (dealer_id, product_id, stock, price)
SELECT u.id,
       p.id,
       (20 + floor(random() * 80))::int,
        ROUND((p.price * (0.90 + random() * 0.20))::numeric, 2)
FROM users u
CROSS JOIN products p
WHERE u.role = 'Bayi';


-- Ornek hareketler — 'bayi' kullanicisi icin, gecmis tarihlere yayilmis
INSERT INTO stock_movements (dealer_id, product_id, quantity, created_at)
SELECT u.id, v.product_id, v.quantity, NOW() - (v.days_ago || ' days')::interval
FROM users u
CROSS JOIN (VALUES
    (1,  50, 6),
    (1, -20, 6),
    (2,  30, 5),
    (3, -15, 4),
    (1,  40, 3),
    (2, -10, 2),
    (4,  25, 1)
) AS v(product_id, quantity, days_ago)
WHERE u.username = 'bayi';


-- Demo talepler: her durumdan en az bir ornek
INSERT INTO requests (dealer_id, product_id, old_price, new_price, status, admin_note, created_at, resolved_at)
SELECT u.id, v.product_id, v.old_price, v.new_price, v.status, v.admin_note,
       NOW() - (v.days_ago || ' days')::interval,
       CASE WHEN v.status = 'pending' THEN NULL
            ELSE NOW() - (v.days_ago || ' days')::interval + interval '2 hours'
       END
FROM users u
CROSS JOIN (VALUES
    (1, 32999.99, 34500.00, 'approved',  'Uygun fiyat',        5),
    (3,   899.99,  1200.00, 'rejected',  'Fiyat cok yuksek',   4),
    (4,   199.90,   185.00, 'approved',  NULL,                 3),
    (6,    34.90,    39.90, 'cancelled', NULL,                 2),
    (8,  6190.00,  6450.00, 'pending',   NULL,                 1)
) AS v(product_id, old_price, new_price, status, admin_note, days_ago)
WHERE u.username = 'bayi';
