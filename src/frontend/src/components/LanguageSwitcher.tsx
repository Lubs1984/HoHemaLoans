import React, { useState, Fragment } from 'react';
import { useTranslation } from 'react-i18next';
import { Listbox, Transition } from '@headlessui/react';
import { CheckIcon, ChevronUpDownIcon } from '@heroicons/react/20/solid';
import { GlobeAltIcon } from '@heroicons/react/24/outline';
import { languages } from '../i18n/config';
import { updateUserLanguagePreference } from '../services/userService';
import { useAuthStore } from '../store/authStore';

interface LanguageSwitcherProps {
  variant?: 'dropdown' | 'modal' | 'inline';
  showLabel?: boolean;
  className?: string;
}

export const LanguageSwitcher: React.FC<LanguageSwitcherProps> = ({ 
  variant = 'dropdown',
  showLabel = true,
  className = ''
}) => {
  const { i18n } = useTranslation('common');
  const user = useAuthStore(state => state.user);
  const [isUpdating, setIsUpdating] = useState(false);

  const currentLanguage = languages.find(lang => lang.code === i18n.language) || languages[0];

  const handleLanguageChange = async (languageCode: string) => {
    setIsUpdating(true);
    try {
      // Update i18n
      await i18n.changeLanguage(languageCode);
      
      // Update localStorage
      localStorage.setItem('preferred_language', languageCode);
      
      // Update backend if user is logged in
      if (user?.id) {
        await updateUserLanguagePreference(user.id, languageCode);
      }
    } catch (error) {
      console.error('Failed to change language:', error);
    } finally {
      setIsUpdating(false);
    }
  };

  if (variant === 'dropdown') {
    return (
      <Listbox value={currentLanguage.code} onChange={handleLanguageChange}>
        <div className={`relative ${className}`}>
          <Listbox.Button className="relative w-full cursor-pointer rounded-lg bg-white py-2 pl-3 pr-10 text-left shadow-md focus:outline-none focus-visible:border-indigo-500 focus-visible:ring-2 focus-visible:ring-white focus-visible:ring-opacity-75 focus-visible:ring-offset-2 focus-visible:ring-offset-blue-300 sm:text-sm">
            <span className="flex items-center gap-2">
              <GlobeAltIcon className="h-5 w-5 text-gray-400" />
              <span className="block truncate">
                {currentLanguage.flag} {showLabel && currentLanguage.nativeName}
              </span>
            </span>
            <span className="pointer-events-none absolute inset-y-0 right-0 flex items-center pr-2">
              <ChevronUpDownIcon className="h-5 w-5 text-gray-400" aria-hidden="true" />
            </span>
          </Listbox.Button>
          
          <Transition
            as={Fragment}
            leave="transition ease-in duration-100"
            leaveFrom="opacity-100"
            leaveTo="opacity-0"
          >
            <Listbox.Options className="absolute z-50 mt-1 max-h-96 w-full min-w-[250px] overflow-auto rounded-md bg-white py-1 text-base shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none sm:text-sm right-0">
              {languages.map((language) => (
                <Listbox.Option
                  key={language.code}
                  className={({ active }) =>
                    `relative cursor-pointer select-none py-2 pl-10 pr-4 ${
                      active ? 'bg-blue-100 text-blue-900' : 'text-gray-900'
                    }`
                  }
                  value={language.code}
                >
                  {({ selected }) => (
                    <>
                      <span className={`block truncate ${selected ? 'font-medium' : 'font-normal'}`}>
                        {language.flag} {language.nativeName}
                        <span className="ml-2 text-xs text-gray-500">({language.name})</span>
                      </span>
                      {selected && (
                        <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-blue-600">
                          <CheckIcon className="h-5 w-5" aria-hidden="true" />
                        </span>
                      )}
                    </>
                  )}
                </Listbox.Option>
              ))}
            </Listbox.Options>
          </Transition>
        </div>
      </Listbox>
    );
  }

  // Grid view for mobile/modal
  if (variant === 'modal') {
    return (
      <div className={`grid grid-cols-2 gap-3 ${className}`}>
        {languages.map((language) => (
          <button
            key={language.code}
            onClick={() => handleLanguageChange(language.code)}
            disabled={isUpdating}
            className={`
              relative rounded-lg border-2 p-4 text-left transition-all
              ${currentLanguage.code === language.code
                ? 'border-blue-500 bg-blue-50 ring-2 ring-blue-200'
                : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50'
              }
              ${isUpdating ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}
            `}
          >
            <div className="flex items-center justify-between">
              <div>
                <span className="text-2xl">{language.flag}</span>
                <p className="mt-1 font-medium text-gray-900">{language.nativeName}</p>
                <p className="text-xs text-gray-500">{language.name}</p>
              </div>
              {currentLanguage.code === language.code && (
                <CheckIcon className="h-6 w-6 text-blue-600" />
              )}
            </div>
          </button>
        ))}
      </div>
    );
  }

  // Inline variant (for settings page)
  return (
    <div className={`space-y-2 ${className}`}>
      {languages.map((language) => (
        <label
          key={language.code}
          className={`
            flex items-center gap-3 rounded-lg border p-3 cursor-pointer transition-colors
            ${currentLanguage.code === language.code
              ? 'border-blue-500 bg-blue-50'
              : 'border-gray-200 hover:bg-gray-50'
            }
          `}
        >
          <input
            type="radio"
            name="language"
            value={language.code}
            checked={currentLanguage.code === language.code}
            onChange={(e) => handleLanguageChange(e.target.value)}
            className="h-4 w-4 text-blue-600 focus:ring-blue-500"
          />
          <span className="text-2xl">{language.flag}</span>
          <div className="flex-1">
            <p className="font-medium text-gray-900">{language.nativeName}</p>
            <p className="text-sm text-gray-500">{language.name}</p>
          </div>
        </label>
      ))}
    </div>
  );
};
