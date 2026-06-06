-- Migration: 003_AddRefreshTokens
-- Description: Create refresh token storage table for JWT refresh workflow
-- Date: 2026-06-06

CREATE TABLE IF NOT EXISTS refreshtokens (
    id SERIAL PRIMARY KEY,
    userid INTEGER NOT NULL,
    token TEXT NOT NULL,
    expiresat TIMESTAMP NOT NULL,
    createdat TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    revokedat TIMESTAMP,
    CONSTRAINT fk_refreshtokens_users_userid FOREIGN KEY (userid) 
        REFERENCES users (id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_refreshtokens_token ON refreshtokens (token);
CREATE INDEX IF NOT EXISTS ix_refreshtokens_userid ON refreshtokens (userid);
