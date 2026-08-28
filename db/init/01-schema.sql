-- ============================================================
-- stok-panel — Veritabanı seed dosyası
-- Veritabanı ilk kez oluşturulduğunda (boş volume) otomatik çalışır.
--
-- Tablolar foreign key sırasına göre kurulur:
--   categories, users -> products -> dealer_stock, stock_movements,
--   requests, login_logs, cart_items
--
-- Demo kullanıcıları (BCrypt hash'li şifreler):
--   admin / admin123   (Admin)
--   bayi  / bayi123    (Bayi)
--   bayi2 / bayi123    (Bayi)
--   bayi3 / bayi123    (Bayi)
--   user1 / user123    (Kullanici)
--   user2 / user123    (Kullanici)
-- ============================================================


-- ---------- 1. KATEGORİLER ----------
-- parent_id: alt kategori desteği (NULL = üst kategori)
CREATE TABLE categories (
    id        SERIAL PRIMARY KEY,
    name      VARCHAR(100) NOT NULL,
    parent_id INT REFERENCES categories(id)
);

INSERT INTO categories (name, parent_id) VALUES
    ('Elektronik', NULL),
    ('Kitap',      NULL),
    ('Gıda',       NULL);

-- Alt kategoriler (üstteki id'lere bağlı)
INSERT INTO categories (name, parent_id) VALUES
    ('Telefon',   1),
    ('Bilgisayar', 1),
    ('Roman',     2);

-- Ek alt kategoriler (mağaza/kategori ekranlarını biraz daha zenginleştirmek için)
INSERT INTO categories (name, parent_id) VALUES
    ('Kulaklık',     1),
    ('Bilim Kurgu',  2);

-- Üst kategorinin altında "açıkta" kalan ürünler için daha spesifik alt kategoriler
INSERT INTO categories (name, parent_id) VALUES
    ('Monitör',             1),
    ('Çikolata',             3),
    ('Meyve & Kuruyemiş',    3),
    ('İçecek',               3);


-- ---------- 2. KULLANICILAR ----------
CREATE TABLE users (
    id            SERIAL PRIMARY KEY,
    username      VARCHAR(50) UNIQUE NOT NULL,
    password_hash VARCHAR(200) NOT NULL,
    role          VARCHAR(20) NOT NULL,
    token         VARCHAR(200),
    token_issued_at TIMESTAMPTZ,
    address       VARCHAR(300),
    phone         VARCHAR(20),
    avatar_url    VARCHAR(500),
    email         VARCHAR(200) UNIQUE
);

INSERT INTO users (username, password_hash, role, address, phone, avatar_url, email) VALUES
    ('admin', '$2a$11$/HEWwh0XFN2vBgwD7DsbFOoxQPtvtw5ChTTOi2c0i0uLReO8d4JH2', 'Admin',     NULL,                NULL,             NULL, 'admin@intshop.local'),
    ('bayi',  '$2a$11$4nMDaYWhemDQOc5K5.C4a.gJxn89kekAOjcTAfWz/MTt0wDb7hoz2', 'Bayi',      'Istanbul, Tuzla',   '0532 111 22 33', NULL, 'bayi@intshop.local'),
    ('bayi2', '$2a$11$4nMDaYWhemDQOc5K5.C4a.gJxn89kekAOjcTAfWz/MTt0wDb7hoz2', 'Bayi',      'Istanbul, Kartal',  '0532 111 44 55', NULL, 'bayi2@intshop.local'),
    ('bayi3', '$2a$11$nUeo6B4.gwLflsYYzOOo0evbBnv6C17FFJn6/9FcrZNDhBDVIhFN2', 'Bayi',      'Istanbul, Kadikoy', '0538 666 77 88', NULL, 'bayi3@intshop.local'),
    ('user1', '$2a$11$KM/OVZMbSJPZYNrU60ZSpuIKl1jQ15buBKlSgqI3yKifLwhuKf13q', 'Kullanici', 'Ankara, Cankaya',   '0533 222 33 44', NULL, 'user1@intshop.local'),
    ('user2', '$2a$11$KM/OVZMbSJPZYNrU60ZSpuIKl1jQ15buBKlSgqI3yKifLwhuKf13q', 'Kullanici', 'Izmir, Bornova',    '0534 555 66 77', NULL, 'user2@intshop.local');


-- ---------- 3. ÜRÜNLER ----------
-- min_oran / max_oran: bayinin taban fiyata gore uygulayabilecegi
-- yuzde araligi (80 = %80 alt sinir, 120 = %120 ust sinir)
CREATE TABLE products (
    id          SERIAL PRIMARY KEY,
    name        VARCHAR(150) NOT NULL,
    category_id INT REFERENCES categories(id),
    stock       INT NOT NULL DEFAULT 0,
    price       NUMERIC(10,2) NOT NULL,
    created_at  TIMESTAMP DEFAULT NOW(),
    image_url   VARCHAR(500),
    min_oran    NUMERIC(5,2) DEFAULT 80,
    max_oran    NUMERIC(5,2) DEFAULT 120,
    attributes  JSONB DEFAULT '{}'::jsonb
);

INSERT INTO products (name, category_id, stock, price, image_url) VALUES
    ('Samsung Galaxy S24',  4, 50,  32999.99, 'https://placehold.co/200x200?text=Galaxy+S24'),
    ('Lenovo IdeaPad 3',    5, 30,  18999.99, 'https://placehold.co/200x200?text=IdeaPad+3'),
    ('Philips Kulaklik',    7, 100,   899.99, 'https://placehold.co/200x200?text=Kulaklik'),
    ('Anna Karenina',       6, 200,   199.90, 'https://placehold.co/200x200?text=Anna+Karenina'),
    ('Sefiller',            6, 120,   249.90, 'https://placehold.co/200x200?text=Sefiller'),
    ('Schogetten Cikolata', 10, 500,    34.90, 'https://placehold.co/200x200?text=Cikolata'),
    ('Amasya Elmasi 1 KG',  11, 300,    49.90, 'https://placehold.co/200x200?text=Elma'),
    ('ASUS Monitor 24"',    9, 40,   6190.00, 'https://placehold.co/200x200?text=Monitor'),
    ('iPhone 15 Pro',           4, 40,  64999.99, '/products/iphone.jpg'),
    ('Logitech Kablosuz Mouse', 5, 150,   799.90, '/products/logitech.jpg'),
    ('MSI Gaming Laptop',       5, 25,  45999.99, '/products/msi.jpg'),
    ('Dune',                    6, 180,   224.90, '/products/dune.jpg'),
    ('Findik 1 KG',             11, 300,    89.90, '/products/findik.jpg'),
    ('Sutlu Cikolata',          10, 400,    59.90, '/products/chocolate.jpg');

-- Yukarıdaki ürünlerden bazıları özellik (attributes) içermeden eklenmişti;
-- yeni alt kategorilerine uygun örnek özellikler ekleniyor.
UPDATE products SET attributes = '{"baglanti":"Kablolu (3.5mm)","garanti":"1 yıl"}'::jsonb
    WHERE name = 'Philips Kulaklik';
UPDATE products SET attributes = '{"ekran":"24 inç","cozunurluk":"1920x1080","panel_tipi":"IPS","yenileme_hizi":"75Hz"}'::jsonb
    WHERE name = 'ASUS Monitor 24"';
UPDATE products SET attributes = '{"agirlik":"100g","skt":"10 ay","icindekiler":"Kakao, süt, şeker"}'::jsonb
    WHERE name = 'Schogetten Cikolata';
UPDATE products SET attributes = '{"agirlik":"80g","skt":"8 ay","icindekiler":"Süt, kakao, şeker"}'::jsonb
    WHERE name = 'Sutlu Cikolata';
UPDATE products SET attributes = '{"agirlik":"1 KG","skt":"3 hafta","icindekiler":"Taze elma"}'::jsonb
    WHERE name = 'Amasya Elmasi 1 KG';
UPDATE products SET attributes = '{"agirlik":"1 KG","skt":"12 ay","icindekiler":"Kabuksuz iç fındık"}'::jsonb
    WHERE name = 'Findik 1 KG';

-- Ek ürünler (kategoriye özel attributes ile, mağaza ekranını biraz daha doldurmak için)
INSERT INTO products (name, category_id, stock, price, image_url, attributes) VALUES
    ('Samsung Galaxy A55', 4, 45, 21999.90, 'https://placehold.co/200x200?text=Galaxy+A55',
        '{"ram":"8GB","depolama":"128GB","ekran":"6.6 inç","batarya":"5000mAh","kamera":"50MP"}'::jsonb),
    ('Google Pixel 8', 4, 20, 39999.00, 'https://placehold.co/200x200?text=Pixel+8',
        '{"ram":"8GB","depolama":"128GB","ekran":"6.2 inç","batarya":"4575mAh","kamera":"50MP"}'::jsonb),
    ('Apple MacBook Air M2', 5, 15, 54999.00, 'https://placehold.co/200x200?text=MacBook+Air',
        '{"islemci":"Apple M2","ram":"8GB","ekran_karti":"8 çekirdekli GPU","depolama":"256GB SSD","ekran":"13.6 inç"}'::jsonb),
    ('Dell XPS 13', 5, 12, 47999.00, 'https://placehold.co/200x200?text=Dell+XPS+13',
        '{"islemci":"Intel Core i7","ram":"16GB","ekran_karti":"Intel Iris Xe","depolama":"512GB SSD","ekran":"13.4 inç"}'::jsonb),
    ('Suç ve Ceza', 6, 150, 189.90, 'https://placehold.co/200x200?text=Suc+ve+Ceza',
        '{"yazar":"Dostoyevski","cevirmen":"Nihal Yalaza Taluy","yayinevi":"İş Bankası Kültür Yayınları","basim_yili":"2019","basim_yeri":"İstanbul","sayfa_sayisi":"687"}'::jsonb),
    ('1984', 6, 200, 159.90, 'https://placehold.co/200x200?text=1984',
        '{"yazar":"George Orwell","cevirmen":"Celal Üster","yayinevi":"Can Yayınları","basim_yili":"2021","basim_yeri":"İstanbul","sayfa_sayisi":"352"}'::jsonb),
    ('Zaman Makinesi', 8, 90, 149.90, 'https://placehold.co/200x200?text=Zaman+Makinesi',
        '{"yazar":"H.G. Wells","cevirmen":"Belkıs Çorakçı Dişbudak","yayinevi":"İthaki Yayınları","basim_yili":"2020","basim_yeri":"İstanbul","sayfa_sayisi":"176"}'::jsonb),
    ('Vakıf', 8, 70, 219.90, 'https://placehold.co/200x200?text=Vakif',
        '{"yazar":"Isaac Asimov","cevirmen":"Levent Cinemre","yayinevi":"İthaki Yayınları","basim_yili":"2022","basim_yeri":"İstanbul","sayfa_sayisi":"296"}'::jsonb),
    ('Nutella 350g', 10, 250, 129.90, 'https://placehold.co/200x200?text=Nutella',
        '{"agirlik":"350g","skt":"12 ay","icindekiler":"Fındık, kakao, süt tozu"}'::jsonb),
    ('Coca Cola 1L', 12, 400, 34.90, 'https://placehold.co/200x200?text=Coca+Cola',
        '{"agirlik":"1 L","skt":"9 ay","icindekiler":"Su, şeker, karbondioksit, aroma"}'::jsonb),
    ('Sony WH-1000XM5', 7, 30, 12999.00, 'https://placehold.co/200x200?text=Sony+WH-1000XM5',
        '{"baglanti":"Bluetooth 5.2 / Kablolu","garanti":"2 yıl"}'::jsonb),
    ('JBL Flip 6 Bluetooth Hoparlör', 7, 55, 3499.00, 'https://placehold.co/200x200?text=JBL+Flip+6',
        '{"baglanti":"Bluetooth 5.1","garanti":"1 yıl"}'::jsonb);


-- ---------- 4. BAYİ STOKLARI ----------
-- price: bayinin kendi satis fiyati. NULL ise urun magazada gorunmez.
CREATE TABLE dealer_stock (
    dealer_id  INT REFERENCES users(id),
    product_id INT REFERENCES products(id),
    stock      INT NOT NULL DEFAULT 0,
    price      NUMERIC(10,2),
    PRIMARY KEY (dealer_id, product_id)
);

-- Her bayiye her urunden stok + taban fiyatin %90-%110 arasinda satis fiyati
INSERT INTO dealer_stock (dealer_id, product_id, stock, price)
SELECT u.id,
       p.id,
       (20 + floor(random() * 80))::int,
        ROUND((p.price * (0.90 + random() * 0.20))::numeric, 2)
FROM users u
CROSS JOIN products p
WHERE u.role = 'Bayi';


-- ---------- 5. STOK HAREKETLERİ ----------
CREATE TABLE stock_movements (
    id         SERIAL PRIMARY KEY,
    dealer_id  INT REFERENCES users(id),
    product_id INT REFERENCES products(id),
    quantity   INT NOT NULL,
    created_at TIMESTAMP DEFAULT NOW()
);

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


-- ---------- 6. FİYAT TALEPLERİ ----------
-- Bayi fiyat degisikligi ister, admin onaylar/reddeder.
-- status: pending | approved | rejected | cancelled
CREATE TABLE requests (
    id          SERIAL PRIMARY KEY,
    dealer_id   INT REFERENCES users(id),
    product_id  INT REFERENCES products(id),
    type        VARCHAR(20) NOT NULL DEFAULT 'price',
    old_price   NUMERIC(10,2),
    new_price   NUMERIC(10,2),
    status      VARCHAR(20) NOT NULL DEFAULT 'pending',
    admin_note  TEXT,
    created_at  TIMESTAMP DEFAULT NOW(),
    resolved_at TIMESTAMP
);

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


-- ---------- 7. LOGLAR ----------
-- action: logs
CREATE TABLE audit_log (
    id          SERIAL PRIMARY KEY,
    actor_id    INT REFERENCES users(id),      -- kim
    actor_role  VARCHAR(20),                   -- o andaki rolü
    action      VARCHAR(50) NOT NULL,          -- ne yaptı: 'product.create'
    entity      VARCHAR(50) NOT NULL,          -- neye: 'products'
    entity_id   INT,                           -- hangi kayda
    old_value   JSONB,                         -- önceki hali
    new_value   JSONB,                         -- sonraki hali
    ip_address  VARCHAR(45),
    created_at  TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_audit_created ON audit_log (created_at DESC);
CREATE INDEX idx_audit_actor ON audit_log (actor_id);


-- ---------- 8. SEPET ----------
-- UNIQUE (user_id, product_id, dealer_id):
-- ayni urun farkli bayilerden ayri satir olarak eklenebilir,
-- ayni ucluden ikinci kayit acilmaz — adet artar (ON CONFLICT).
CREATE TABLE cart_items (
    id         SERIAL PRIMARY KEY,
    user_id    INT NOT NULL REFERENCES users(id),
    product_id INT NOT NULL REFERENCES products(id),
    dealer_id  INT NOT NULL REFERENCES users(id),
    quantity   INT NOT NULL DEFAULT 1,
    added_at   TIMESTAMP DEFAULT NOW(),
    UNIQUE (user_id, product_id, dealer_id)
);