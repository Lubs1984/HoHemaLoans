-- Add language preference support to HoHema Loans database
-- This migration adds columns and tables for storing user language preferences

-- Step 1: Add preferred_language column to users table
ALTER TABLE users 
ADD COLUMN IF NOT EXISTS preferred_language VARCHAR(10) DEFAULT 'en';

-- Step 2: Create index for performance
CREATE INDEX IF NOT EXISTS idx_users_preferred_language 
ON users(preferred_language);

-- Step 3: Create user_language_preferences table for detailed tracking
CREATE TABLE IF NOT EXISTS user_language_preferences (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    language_code VARCHAR(10) NOT NULL,
    set_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    set_via VARCHAR(50), -- 'manual', 'auto-detect', 'whatsapp'
    is_active BOOLEAN DEFAULT true,
    UNIQUE(user_id, language_code)
);

-- Step 4: Create index on user_id for faster lookups
CREATE INDEX IF NOT EXISTS idx_user_language_preferences_user_id 
ON user_language_preferences(user_id);

-- Step 5: Add or update system settings for supported languages
INSERT INTO system_settings (key, value, description) 
VALUES 
    ('supported_languages', '["en","af","zu","xh","nso","tn","st","ts","ss","ve","nr"]', 'List of supported language codes'),
    ('default_language', 'en', 'Default language for new users'),
    ('language_fallback', 'en', 'Fallback language when translation is missing')
ON CONFLICT (key) 
DO UPDATE SET 
    value = EXCLUDED.value,
    description = EXCLUDED.description;

-- Step 6: Update existing users to have default language
UPDATE users 
SET preferred_language = 'en' 
WHERE preferred_language IS NULL;

-- Verification queries (run these to verify the migration)
-- SELECT COUNT(*) FROM users WHERE preferred_language IS NOT NULL;
-- SELECT * FROM system_settings WHERE key LIKE '%language%';
-- SELECT COUNT(*) FROM user_language_preferences;

COMMIT;
