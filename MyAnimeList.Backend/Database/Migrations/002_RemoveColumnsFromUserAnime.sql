-- Migration: 002_RemoveColumnsFromUserAnime
-- Description: Remove score, startdate, finishdate from useranime table
-- Date: 2026-06-05

ALTER TABLE IF EXISTS useranime
    DROP COLUMN IF EXISTS score,
    DROP COLUMN IF EXISTS startdate,
    DROP COLUMN IF EXISTS finishdate;
