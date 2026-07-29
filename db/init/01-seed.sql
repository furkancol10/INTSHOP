CREATE TABLE categories (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL
);

CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    name VARCHAR(150) NOT NULL,
    category_id INT REFERENCES categories(id),
    stock INT NOT NULL DEFAULT 0,
    price NUMERIC(10,2) NOT NULL,
    created_at TIMESTAMP DEFAULT NOW()
);

INSERT INTO categories (name) VALUES ('electronics'), ('Bookstore'), ('Food');

INSERT INTO products (name, category_id, stock, price) VALUES
('Smartphone', 1, 50, 699.99),
('Laptop', 1, 30, 999.99),
('Headphones', 1, 100, 199.99),
('Fiction Book', 2, 200, 14.99),
('Non-fiction Book', 2, 5, 19.99),
('Chocolate Bar', 3, 50, 1.49),
('Organic Apples', 3, 300, 2.99);