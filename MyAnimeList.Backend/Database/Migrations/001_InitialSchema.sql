-- Migration: 001_InitialSchema
-- Description: Creates initial database schema with Anime, Users, and UserAnime tables
-- Date: 2026-05-19
-- Note: PostgreSQL converts unquoted identifiers to lowercase automatically.
--       We use lowercase for clean SQL. See Database/POSTGRESQL_NAMING.md for details.

-- Create Anime table
CREATE TABLE IF NOT EXISTS anime (
    id SERIAL PRIMARY KEY,
    malid INTEGER NOT NULL UNIQUE,
    title TEXT NOT NULL,
    englishtitle TEXT,
    synopsis TEXT,
    type TEXT,
    episodes INTEGER NOT NULL DEFAULT 0,
    status TEXT,
    score DOUBLE PRECISION,
    popularity INTEGER,
    rank INTEGER,
    startdate DATE,
    enddate DATE,
    imageurl TEXT,
    genre TEXT,
    airedfrom TIMESTAMP,
    airedto TIMESTAMP,
    createdat TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updatedat TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_anime_malid ON anime (malid);

-- Create Users table
CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    username TEXT NOT NULL UNIQUE,
    email TEXT NOT NULL UNIQUE,
    passwordhash TEXT NOT NULL,
    createdat TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_users_email ON users (email);
CREATE INDEX IF NOT EXISTS ix_users_username ON users (username);

-- Create UserAnime table (using MalId from the start)
CREATE TABLE IF NOT EXISTS useranime (
    id SERIAL PRIMARY KEY,
    userid INTEGER NOT NULL,
    malid INTEGER NOT NULL,
    status INTEGER NOT NULL,
    score DOUBLE PRECISION,
    userscore INTEGER,
    episodeswatched INTEGER,
    startdate DATE,
    finishdate DATE,
    notes TEXT,
    dateadded TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    dateupdated TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_useranime_users_userid FOREIGN KEY (userid) 
        REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_useranime_anime_malid FOREIGN KEY (malid) 
        REFERENCES anime (malid) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_useranime_userid ON useranime (userid);
CREATE INDEX IF NOT EXISTS ix_useranime_malid ON useranime (malid);
CREATE UNIQUE INDEX IF NOT EXISTS ix_useranime_userid_malid ON useranime (userid, malid);

-- Create AnimeTitles table
CREATE TABLE IF NOT EXISTS animetitles (
    id SERIAL PRIMARY KEY,
    malid INTEGER NOT NULL,
    type TEXT NOT NULL,
    title TEXT NOT NULL,
    CONSTRAINT fk_animetitles_anime_malid FOREIGN KEY (malid) 
        REFERENCES anime (malid) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_animetitles_malid_type ON animetitles (malid, type);

-- Create migration tracking table
CREATE TABLE IF NOT EXISTS sqlmigrations (
    id SERIAL PRIMARY KEY,
    migrationname TEXT NOT NULL UNIQUE,
    appliedat TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
