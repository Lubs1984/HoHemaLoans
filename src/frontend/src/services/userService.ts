import { apiService } from './api';

export interface UserLanguagePreference {
  userId: string;
  languageCode: string;
  setAt?: Date;
  setVia?: string;
}

/**
 * Update user's language preference
 */
export const updateUserLanguagePreference = async (
  userId: string,
  languageCode: string
): Promise<void> => {
  try {
    await apiService.request(`/users/${userId}/language`, {
      method: 'PUT',
      body: JSON.stringify({
        languageCode,
        setVia: 'manual'
      })
    });
  } catch (error) {
    console.error('Failed to update language preference:', error);
    // Don't throw - allow the app to continue even if backend update fails
  }
};

/**
 * Get user's language preference
 */
export const getUserLanguagePreference = async (
  userId: string
): Promise<string | null> => {
  try {
    const response = await apiService.request<{ languageCode: string }>(`/users/${userId}/language`, {
      method: 'GET'
    });
    return response.languageCode;
  } catch (error) {
    console.error('Failed to get language preference:', error);
    return null;
  }
};
