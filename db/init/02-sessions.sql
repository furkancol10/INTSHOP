-- ============================================================
-- stok-panel — Oturum (sessions) tablosu
--
-- Eski tasarımda tek bir ham token, users.token sütununda düz metin
-- olarak tutuluyordu (hash yok, iptal/çoklu oturum imkânı yok, indekssiz).
-- Bunun yerine: ham token yalnızca istemciye döner, veritabanına sadece
-- SHA-256 özeti yazılır; her giriş yeni bir satır oluşturur ve aynı
-- kullanıcının önceki aktif oturumlarını iptal eder (revoked_at).
-- ============================================================

CREATE TABLE sessions (
    id          SERIAL PRIMARY KEY,
    user_id     INT NOT NULL REFERENCES users(id),
    token_hash  VARCHAR(64) NOT NULL,
    issued_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at  TIMESTAMPTZ NOT NULL,
    revoked_at  TIMESTAMPTZ,
    user_agent  VARCHAR(300),
    ip_address  VARCHAR(45)
);

CREATE UNIQUE INDEX ix_sessions_token_hash ON sessions(token_hash);
CREATE INDEX ix_sessions_user_id_active ON sessions(user_id) WHERE revoked_at IS NULL;
