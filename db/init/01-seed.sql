-- ============================================================
-- stok-panel — Veritabanı seed dosyası
-- Veritabanı ilk kez oluşturulduğunda (boş volume) otomatik çalışır.
-- Tablolar foreign key sırasına göre kurulur:
--   categories, users → products → dealer_stock, stock_movements
--
-- Kullanıcı şifreleri (BCrypt hash'li):
--   admin / admin123   (Admin)
--   bayi1  / bayi123    (Bayi)
--   bayi2 / bayi123    (Bayi)
--   bayi3 / bayi123    (Bayi)
--   user1 / user123    (Kullanici)
-- ============================================================

-- ---------- 1. KATEGORİLER ----------
CREATE TABLE categories (
    id   SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL
);

INSERT INTO categories (name) VALUES
    ('electronics'),
    ('Bookstore'),
    ('Food');

-- ---------- 2. ÜRÜNLER ----------
CREATE TABLE products (
    id          SERIAL PRIMARY KEY,
    name        VARCHAR(150) NOT NULL,
    category_id INT REFERENCES categories(id),
    stock       INT NOT NULL DEFAULT 0,
    price       NUMERIC(10,2) NOT NULL,
    created_at  TIMESTAMP DEFAULT NOW(),
    image_url   VARCHAR(500)
);

INSERT INTO products (name, category_id, stock, price, image_url) VALUES
    ('Smartphone',       1, 50,  699.99, 'https://placehold.co/200x200?text=Smartphone'),
    ('Laptop',           1, 30,  999.99, 'https://placehold.co/200x200?text=Laptop'),
    ('Headphones',       1, 100, 199.99, 'https://placehold.co/200x200?text=Headphones'),
    ('Fiction Book',     2, 200, 14.99,  'https://placehold.co/200x200?text=Fiction+Book'),
    ('Non-fiction Book', 2, 5,   19.99,  'https://placehold.co/200x200?text=Non-fiction+Book'),
    ('Chocolate Bar',    3, 50,  1.49,   'https://placehold.co/200x200?text=Chocolate+Bar'),
    ('Organic Apples',   3, 300, 2.99,   'https://placehold.co/200x200?text=Organic+Apples');

-- ---------- 3. KULLANICILAR ----------
-- password_hash: BCrypt. Sifreler admin123 / bayi123 / user123
CREATE TABLE users (
    id            SERIAL PRIMARY KEY,
    username      VARCHAR(50) UNIQUE NOT NULL,
    password_hash VARCHAR(200) NOT NULL,
    role          VARCHAR(20) NOT NULL,
    token         VARCHAR(200),
    address       VARCHAR(300),
    phone         VARCHAR(20)
);

INSERT INTO users (username, password_hash, role, address, phone) VALUES
    ('admin', '$2a$11$/HEWwh0XFN2vBgwD7DsbFOoxQPtvtw5ChTTOi2c0i0uLReO8d4JH2', 'Admin',     NULL,                NULL),
    ('bayi',  '$2a$11$qddF251nRT8zne3vzE5FGehCRrD/y7Xyk5sNVWlNnn4q5ylRJMC1m', 'Bayi',      'Istanbul, Tuzla',   '0532 111 22 33'),
    ('bayi2', '$2a$11$4nMDaYWhemDQOc5K5.C4a.gJxn89kekAOjcTAfWz/MTt0wDb7hoz2', 'Bayi',      'Istanbul, Kartal',  '0532 111 44 55'),
    ('bayi3', '$2a$11$nUeo6B4.gwLflsYYzOOo0evbBnv6C17FFJn6/9FcrZNDhBDVIhFN2', 'Bayi',      'Istanbul, Kadikoy', '0538 666 77 88'),
    ('user1', '$2a$11$tH0cAyacwEi6m4As759wou2qmYO3lxYpSWnym5zltD4s6q/Kdveyu', 'Kullanici', NULL,                NULL);

-- ---------- 4. BAYI STOKLARI ----------
CREATE TABLE dealer_stock (
    dealer_id  INT REFERENCES users(id),
    product_id INT REFERENCES products(id),
    stock      INT NOT NULL DEFAULT 0,
    PRIMARY KEY (dealer_id, product_id)
);

-- Her bayiye her urunden rastgele stok (CROSS JOIN)
INSERT INTO dealer_stock (dealer_id, product_id, stock)
SELECT u.id, p.id, floor(random() * 100)::int
FROM users u
CROSS JOIN products p
WHERE u.role = 'Bayi';

-- ---------- 5. STOK HAREKETLERI ----------
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