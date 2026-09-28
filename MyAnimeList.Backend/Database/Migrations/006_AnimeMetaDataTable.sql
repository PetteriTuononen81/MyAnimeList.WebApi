CREATE TABLE IF NOT EXISTS animemetadata (
    id SERIAL PRIMARY KEY,
    malid INTEGER NOT NULL UNIQUE,
    demographic TEXT,
    themes TEXT[] NOT NULL DEFAULT '{}',
    genres TEXT[] NOT NULL DEFAULT '{}',
    lastupdatedutc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_animemetadata_anime_malid FOREIGN KEY (malid) 
        REFERENCES anime (malid) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_animemetadata_malid ON animemetadata (malid);