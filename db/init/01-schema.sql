-- ============================================================
-- stok-panel — Şema (DDL) dosyası
-- Veritabanı ilk kez oluşturulduğunda (boş volume) otomatik çalışır.
--
-- Bu dosya sadece tablo tanımlarını ve kimlik bilgisi taşımayan
-- referans verisini (kategoriler, ürün kataloğu) içerir. Şifreli demo
-- hesapları ve onlara bağlı örnek veriler (bayi stoğu, hareketler,
-- talepler) burada YOK — bunlar otomatik çalışmaz, bkz. db/seed/dev-seed.sql.
--
-- Tablolar foreign key sırasına göre kurulur:
--   categories, users -> products -> dealer_stock, stock_movements,
--   requests, login_logs, cart_items
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


-- ---------- 2. KULLANICILAR ----------
CREATE TABLE users (
    id            SERIAL PRIMARY KEY,
    username      VARCHAR(50) UNIQUE NOT NULL,
    password_hash VARCHAR(200) NOT NULL,
    role          VARCHAR(20) NOT NULL,
    address       VARCHAR(300),
    phone         VARCHAR(20),
    avatar_url    VARCHAR(500),
    email         VARCHAR(200) UNIQUE
);
-- Oturum/token bilgisi burada degil, sessions tablosunda tutulur (bkz. 02-sessions.sql).

-- Demo/gelistirme hesaplari icin bkz. db/seed/dev-seed.sql (elle uygulanir,
-- bu dizinde otomatik calismaz). Prod ortaminda ilk admin hesabini elle,
-- kendi belirleyeceginiz bir bcrypt hash ile olusturun.


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
    max_oran    NUMERIC(5,2) DEFAULT 120
);

INSERT INTO products (name, category_id, stock, price, image_url) VALUES
    ('Samsung Galaxy S24',  4, 50,  32999.99, 'https://placehold.co/200x200?text=Galaxy+S24'),
    ('Lenovo IdeaPad 3',    5, 30,  18999.99, 'https://placehold.co/200x200?text=IdeaPad+3'),
    ('Philips Kulaklik',    1, 100,   899.99, 'https://placehold.co/200x200?text=Kulaklik'),
    ('Anna Karenina',       6, 200,   199.90, 'https://placehold.co/200x200?text=Anna+Karenina'),
    ('Sefiller',            6, 120,   249.90, 'https://placehold.co/200x200?text=Sefiller'),
    ('Schogetten Cikolata', 3, 500,    34.90, 'https://placehold.co/200x200?text=Cikolata'),
    ('Amasya Elmasi 1 KG',  3, 300,    49.90, 'https://placehold.co/200x200?text=Elma'),
    ('ASUS Monitor 24"',    1, 40,   6190.00, 'https://placehold.co/200x200?text=Monitor'),
    ('iPhone 15 Pro',           4, 40,  64999.99, '/products/iphone.jpg'),
    ('Logitech Kablosuz Mouse', 5, 150,   799.90, '/products/logitech.jpg'),
    ('MSI Gaming Laptop',       5, 25,  45999.99, '/products/msi.jpg'),
    ('Dune',                    6, 180,   224.90, '/products/dune.jpg'),
    ('Findik 1 KG',             3, 300,    89.90, '/products/findik.jpg'),
    ('Sutlu Cikolata',          3, 400,    59.90, '/products/chocolate.jpg');


-- ---------- 4. BAYİ STOKLARI ----------
-- price: bayinin kendi satis fiyati. NULL ise urun magazada gorunmez.
CREATE TABLE dealer_stock (
    dealer_id  INT REFERENCES users(id),
    product_id INT REFERENCES products(id),
    stock      INT NOT NULL DEFAULT 0,
    price      NUMERIC(10,2),
    PRIMARY KEY (dealer_id, product_id)
);


-- ---------- 5. STOK HAREKETLERİ ----------
CREATE TABLE stock_movements (
    id         SERIAL PRIMARY KEY,
    dealer_id  INT REFERENCES users(id),
    product_id INT REFERENCES products(id),
    quantity   INT NOT NULL,
    created_at TIMESTAMP DEFAULT NOW()
);


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


-- ---------- 7. GİRİŞ / ÇIKIŞ LOGLARI ----------
-- action: login | logout | login_failed
-- user_id: login_failed'de NULL olabilir (kullanici bulunamadi) - bu durumda
-- denenen kullanici adi attempted_username'de tutulur.
CREATE TABLE login_logs (
    id                  SERIAL PRIMARY KEY,
    user_id             INT REFERENCES users(id),
    attempted_username  VARCHAR(50),
    action              VARCHAR(20) NOT NULL,
    ip_address          VARCHAR(45),
    created_at          TIMESTAMP DEFAULT NOW()
);


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


-- ---------- 9. DENETİM İZİ (AUDIT LOG) ----------
-- Admin eylemlerinin (kullanici olusturma, talep onay/red, urun silme, vb.)
-- kaydi. action: 'user_create' | 'request_approve' | 'request_reject' | 'product_delete'
CREATE TABLE audit_log (
    id          SERIAL PRIMARY KEY,
    actor_id    INT REFERENCES users(id),
    action      VARCHAR(50) NOT NULL,
    target_type VARCHAR(50),
    target_id   INT,
    details     JSONB,
    ip_address  VARCHAR(45),
    created_at  TIMESTAMPTZ DEFAULT NOW()
);
