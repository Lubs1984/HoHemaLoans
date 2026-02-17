# Multilingual Support - Quick Start Guide

## Immediate Next Steps

### Step 1: Install Dependencies (5 minutes)

```bash
cd src/frontend
npm install i18next react-i18next i18next-http-backend i18next-browser-languagedetector
```

### Step 2: Create Directory Structure (2 minutes)

```bash
# From src/frontend directory
mkdir -p src/i18n/locales/{en,af,zu,xh,nso,tn,st,ts,ss,ve,nr}
mkdir -p src/i18n/utils
mkdir -p public/locales/{en,af,zu,xh,nso,tn,st,ts,ss,ve,nr}

# Create namespace files for English (template for others)
touch public/locales/en/{common,auth,loan,validation,errors,admin}.json
```

### Step 3: Initialize i18n Config (10 minutes)

Create `src/frontend/src/i18n/config.ts` with the configuration from the main plan document.

### Step 4: Update Main App Entry (5 minutes)

**File: `src/frontend/src/main.tsx`**

```typescript
import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
import './index.css';
import './i18n/config'; // Add this import

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);
```

### Step 5: Create Language Switcher Component (15 minutes)

Copy the `LanguageSwitcher.tsx` component from the main plan document to:
`src/frontend/src/components/LanguageSwitcher.tsx`

### Step 6: Add Language Switcher to Navigation (5 minutes)

Find your main navigation component and add:

```typescript
import { LanguageSwitcher } from './components/LanguageSwitcher';

// In your nav component
<nav>
  {/* existing nav items */}
  <LanguageSwitcher variant="dropdown" showLabel={false} />
</nav>
```

### Step 7: Create Initial Translation Files (20 minutes)

**public/locales/en/common.json:**
```json
{
  "app": {
    "name": "HoHema Loans"
  },
  "navigation": {
    "home": "Home",
    "loans": "Loans",
    "profile": "Profile"
  },
  "actions": {
    "submit": "Submit",
    "cancel": "Cancel"
  }
}
```

**public/locales/af/common.json:**
```json
{
  "app": {
    "name": "HoHema Lenings"
  },
  "navigation": {
    "home": "Tuis",
    "loans": "Lenings",
    "profile": "Profiel"
  },
  "actions": {
    "submit": "Dien in",
    "cancel": "Kanselleer"
  }
}
```

### Step 8: Convert One Page to Use Translations (15 minutes)

Pick a simple page (e.g., Home page) and update it:

**Before:**
```typescript
const HomePage = () => {
  return (
    <div>
      <h1>Welcome to HoHema Loans</h1>
      <button>Apply Now</button>
    </div>
  );
};
```

**After:**
```typescript
import { useTranslation } from 'react-i18next';

const HomePage = () => {
  const { t } = useTranslation('common');
  
  return (
    <div>
      <h1>{t('app.name')}</h1>
      <button>{t('actions.submit')}</button>
    </div>
  );
};
```

### Step 9: Test (5 minutes)

1. Run your development server: `npm run dev`
2. Open the app
3. Click the language switcher
4. Select Afrikaans
5. Verify the text changes

### Step 10: Database Setup (10 minutes)

```bash
# From project root
cd scripts
```

Create `add-language-preference.sql`:
```sql
ALTER TABLE users 
ADD COLUMN IF NOT EXISTS preferred_language VARCHAR(10) DEFAULT 'en';

CREATE INDEX IF NOT EXISTS idx_users_preferred_language 
ON users(preferred_language);
```

Apply it:
```bash
# Adjust connection string as needed
psql -h localhost -U your_user -d hohemals < add-language-preference.sql
```

---

## Testing Checklist

- [ ] Language switcher appears in navigation
- [ ] Can switch between English and Afrikaans
- [ ] Selected language persists on page reload
- [ ] Browser console shows no errors
- [ ] Translations display correctly

---

## Common Issues & Solutions

### Issue: Translations not loading

**Solution:** Check that json files are in `public/locales/` directory, not `src/`

### Issue: "Missing key" errors

**Solution:** Ensure namespace is correct. If using `t('navigation.home')`, you need either:
- Default namespace includes it, OR
- Specify namespace: `t('common:navigation.home')`, OR
- Use hook: `useTranslation(['common'])`

### Issue: Language doesn't persist

**Solution:** Check browser localStorage. The key `preferred_language` should be set.

### Issue: Date/currency not formatting

**Solution:** Ensure interpolation format function is in i18n config (see main document)

---

## Next Steps After Quick Start

1. **Expand Translation Coverage**
   - Identify all text strings in your app
   - Move them to translation files
   - Start with most-used pages

2. **Get Professional Translations**
   - Export all English JSON files
   - Send to professional translation service
   - Import translated files

3. **Add API Endpoint**
   - Create endpoint to save user language preference
   - Update LanguageSwitcher to call this endpoint

4. **WhatsApp Integration**
   - Add language selection to WhatsApp flow
   - Send messages in user's preferred language

5. **Email Templates**
   - Create email templates for each language
   - Update notification service to use correct template

---

## Resources

- **Main Plan:** [MULTILINGUAL_IMPLEMENTATION_PLAN.md](./MULTILINGUAL_IMPLEMENTATION_PLAN.md)
- **i18next Docs:** https://www.i18next.com
- **React i18next:** https://react.i18next.com
- **Translation Services:** Gengo, Smartling, Phrase

---

## Get Help

If you encounter issues:
1. Check browser console for errors
2. Verify file paths match exactly
3. Ensure npm packages installed correctly
4. Check i18n config is imported in main.tsx

---

**Estimated Time to Complete Quick Start:** 1.5 - 2 hours  
**Result:** Working language switcher with English & Afrikaans
