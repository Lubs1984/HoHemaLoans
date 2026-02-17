import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from '../../store/authStore';
import { apiService } from '../../services/api';
import { useToast } from '../../contexts/ToastContext';
import HohemaLogo from '../../assets/hohema-logo.png';
import { LanguageSwitcher } from '../../components/LanguageSwitcher';

interface RegisterFormData {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  idNumber: string;
  dateOfBirth: string;
  address: string;
  monthlyIncome: string;
  agreeToTerms: boolean;
}

const Register = () => {
  const { t } = useTranslation(['auth']);
  const navigate = useNavigate();
  const { login, setLoading, setError, error, isLoading } = useAuthStore();
  const { success, error: showError } = useToast();
  const [formData, setFormData] = useState<RegisterFormData>({
    email: '',
    password: '',
    confirmPassword: '',
    firstName: '',
    lastName: '',
    phoneNumber: '',
    idNumber: '',
    dateOfBirth: '',
    address: '',
    monthlyIncome: '',
    agreeToTerms: false,
  });
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    if (!formData.email) {
      errors.email = t('auth:register.validation.emailRequired');
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      errors.email = t('auth:register.validation.emailInvalid');
    }

    if (!formData.password) {
      errors.password = t('auth:register.validation.passwordRequired');
    } else if (formData.password.length < 6) {
      errors.password = t('auth:register.validation.passwordMin');
    }

    if (formData.password !== formData.confirmPassword) {
      errors.confirmPassword = t('auth:register.validation.passwordsNoMatch');
    }

    if (!formData.firstName) {
      errors.firstName = t('auth:register.validation.firstNameRequired');
    }

    if (!formData.lastName) {
      errors.lastName = t('auth:register.validation.lastNameRequired');
    }

    if (!formData.phoneNumber) {
      errors.phoneNumber = t('auth:register.validation.phoneRequired');
    } else if (!/^\+?[0-9]{10,}$/.test(formData.phoneNumber.replace(/\D/g, ''))) {
      errors.phoneNumber = t('auth:register.validation.phoneInvalid');
    }

    if (!formData.idNumber) {
      errors.idNumber = t('auth:register.validation.idRequired');
    }

    if (!formData.dateOfBirth) {
      errors.dateOfBirth = t('auth:register.validation.dobRequired');
    }

    if (!formData.address) {
      errors.address = t('auth:register.validation.addressRequired');
    }

    if (!formData.monthlyIncome) {
      errors.monthlyIncome = t('auth:register.validation.incomeRequired');
    } else if (isNaN(parseFloat(formData.monthlyIncome))) {
      errors.monthlyIncome = t('auth:register.validation.incomeInvalid');
    }

    if (!formData.agreeToTerms) {
      errors.agreeToTerms = t('auth:register.validation.termsRequired');
    }

    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    try {
      setLoading(true);
      setError(null);

      const response = await apiService.register({
        email: formData.email,
        password: formData.password,
        firstName: formData.firstName,
        lastName: formData.lastName,
        phoneNumber: formData.phoneNumber,
        idNumber: formData.idNumber,
        dateOfBirth: new Date(formData.dateOfBirth),
        address: formData.address,
        monthlyIncome: parseFloat(formData.monthlyIncome),
      });

      if (response && response.token && response.user) {
        const refreshToken = (response as any).refreshToken || '';
        login(response.user, response.token, refreshToken);
        success(`Welcome to HoHema, ${response.user.firstName}! Your account has been created successfully.`);
        navigate('/dashboard', { replace: true });
      } else {
        setError('Invalid response from server');
        showError('Registration failed - invalid response from server');
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to register. Please try again.';
      setError(errorMessage);
      showError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value,
    }));
    if (validationErrors[name]) {
      setValidationErrors((prev) => {
        const updated = { ...prev };
        delete updated[name];
        return updated;
      });
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center px-4 py-8">
      <div className="w-full max-w-2xl relative">
        {/* Language Switcher */}
        <div className="absolute -top-12 right-0">
          <LanguageSwitcher variant="dropdown" showLabel={false} className="" />
        </div>
        
        {/* Header */}
        <div className="text-center mb-8">
          <div className="mx-auto h-16 w-16 flex items-center justify-center mb-4">
            <img src={HohemaLogo} alt="Ho Hema Loans" className="h-16 w-auto" />
          </div>
          <h1 className="text-3xl font-bold text-gray-900 mb-2">{t('auth:register.title')}</h1>
          <p className="text-gray-600">{t('auth:register.subtitle')}</p>
        </div>

        {/* Card */}
        <div className="bg-white rounded-lg shadow-lg p-8">
          {/* Error Message */}
          {error && (
            <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-md">
              <p className="text-red-700 text-sm">{error}</p>
            </div>
          )}

          {/* Form */}
          <form onSubmit={handleSubmit} className="space-y-5">
            {/* Name Row */}
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label htmlFor="firstName" className="block text-sm font-medium text-gray-700 mb-1">
                  {t('auth:register.firstNameLabel')}
                </label>
                <input
                  id="firstName"
                  name="firstName"
                  type="text"
                  value={formData.firstName}
                  onChange={handleChange}
                  disabled={isLoading}
                  className={`w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-colors ${
                    validationErrors.firstName
                      ? 'border-red-500 bg-red-50'
                      : 'border-gray-300 bg-white'
                  } ${isLoading ? 'opacity-60 cursor-not-allowed' : ''}`}
                  placeholder={t('auth:register.firstNamePlaceholder')}
                />
                {validationErrors.firstName && (
                  <p className="mt-1 text-sm text-red-600">{validationErrors.firstName}</p>
                )}
              </div>
              <div>
                <label htmlFor="lastName" className="block text-sm font-medium text-gray-700 mb-1">
                  {t('auth:register.lastNameLabel')}
                </label>
                <input
                  id="lastName"
                  name="lastName"
                  type="text"
                  value={formData.lastName}
                  onChange={handleChange}
                  disabled={isLoading}
                  className={`w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-colors ${
                    validationErrors.lastName
                      ? 'border-red-500 bg-red-50'
                      : 'border-gray-300 bg-white'
                  } ${isLoading ? 'opacity-60 cursor-not-allowed' : ''}`}
                  placeholder={t('auth:register.lastNamePlaceholder')}
                />
                {validationErrors.lastName && (
                  <p className="mt-1 text-sm text-red-600">{validationErrors.lastName}</p>
                )}
              </div>
            </div>

            {/* Email Field */}
            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                {t('auth:register.emailLabel')}
              </label>
              <input
                id="email"
                name="email"
                type="email"
                autoComplete="email"
                value={formData.email}
                onChange={handleChange}
                disabled={isLoading}
                className={`w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-colors ${
                  validationErrors.email
                    ? 'border-red-500 bg-red-50'
                    : 'border-gray-300 bg-white'
                } ${isLoading ? 'opacity-60 cursor-not-allowed' : ''}`}
                placeholder={t('auth:register.emailPlaceholder')}
              />
              {validationErrors.email && (
                <p className="mt-1 text-sm text-red-600">{validationErrors.email}</p>
              )}
            </div>

            {/* Phone and ID Row */}
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label htmlFor="phoneNumber" className="block text-sm font-medium text-gray-700 mb-1">
                  {t('auth:register.phoneLabel')}
                </label>
                <input
                  id="phoneNumber"
                  name="phoneNumber"
                  type="tel"
                  autoComplete="tel"
                  value={formData.phoneNumber}
                  onChange={handleChange}
                  disabled={isLoading}
                  className={`w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-colors ${
                    validationErrors.phoneNumber
                      ? 'border-red-500 bg-red-50'
                      : 'border-gray-300 bg-white'
                  } ${isLoading ? 'opacity-60 cursor-not-allowed' : ''}`}
                  placeholder={t('auth:register.phonePlaceholder')}
                />
                {validationErrors.phoneNumber && (
                  <p className="mt-1 text-sm text-red-600">{validationErrors.phoneNumber}</p>
                )}
              </div>
              <div>
                <label htmlFor="idNumber" className="block text-sm font-medium text-gray-700 mb-1">
                  {t('auth:register.idLabel')}
                </label>
                <input
                  id="idNumber"
                  name="idNumber"
                  type="text"
                  value={formData.idNumber}
                  onChange={handleChange}
                  disabled={isLoading}
                  className={`w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-colors ${
                    validationErrors.idNumber
                      ? 'border-red-500 bg-red-50'
                      : 'border-gray-300 bg-white'
                  } ${isLoading ? 'opacity-60 cursor-not-allowed' : ''}`}
                  placeholder={t('auth:register.idPlaceholder')}
                />
                {validationErrors.idNumber && (
                  <p className="mt-1 text-sm text-red-600">{validationErrors.idNumber}</p>
                )}
              </div>
            </div>

            {/* DOB and Income Row */}
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label htmlFor="dateOfBirth" className="block text-sm font-medium text-gray-700 mb-1">
                  {t('auth:register.dobLabel')}
                </label>
                <input
                  id="dateOfBirth"
                  name="dateOfBirth"
                  type="date"
                  value={formData.dateOfBirth}
                  onChange={handleChange}
                  disabled={isLoading}
                  className={`w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-colors ${
                    validationErrors.dateOfBirth
                      ? 'border-red-500 bg-red-50'
                      : 'border-gray-300 bg-white'
                  } ${isLoading ? 'opacity-60 cursor-not-allowed' : ''}`}
                />
                {validationErrors.dateOfBirth && (
                  <p className="mt-1 text-sm text-red-600">{validationErrors.dateOfBirth}</p>
                )}
              </div>
              <div>
                <label htmlFor="monthlyIncome" className="block text-sm font-medium text-gray-700 mb-1">
                  {t('auth:register.incomeLabel')}
                </label>
                <input
                  id="monthlyIncome"
                  name="monthlyIncome"
                  type="number"
                  step="0.01"
                  value={formData.monthlyIncome}
                  onChange={handleChange}
                  disabled={isLoading}
                  className={`w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-colors ${
                    validationErrors.monthlyIncome
                      ? 'border-red-500 bg-red-50'
                      : 'border-gray-300 bg-white'
                  } ${isLoading ? 'opacity-60 cursor-not-allowed' : ''}`}
                  placeholder={t('auth:register.incomePlaceholder')}
                />
                {validationErrors.monthlyIncome && (
                  <p className="mt-1 text-sm text-red-600">{validationErrors.monthlyIncome}</p>
                )}
              </div>
            </div>

            {/* Address Field */}
            <div>
              <label htmlFor="address" className="block text-sm font-medium text-gray-700 mb-1">
                {t('auth:register.addressLabel')}
              </label>
              <input
                id="address"
                name="address"
                type="text"
                value={formData.address}
                onChange={handleChange}
                disabled={isLoading}
                className={`w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-colors ${
                  validationErrors.address
                    ? 'border-red-500 bg-red-50'
                    : 'border-gray-300 bg-white'
                } ${isLoading ? 'opacity-60 cursor-not-allowed' : ''}`}
                placeholder={t('auth:register.addressPlaceholder')}
              />
              {validationErrors.address && (
                <p className="mt-1 text-sm text-red-600">{validationErrors.address}</p>
              )}
            </div>

            {/* Password Row */}
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
                  {t('auth:register.passwordLabel')}
                </label>
                <input
                  id="password"
                  name="password"
                  type="password"
                  autoComplete="new-password"
                  value={formData.password}
                  onChange={handleChange}
                  disabled={isLoading}
                  className={`w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-colors ${
                    validationErrors.password
                      ? 'border-red-500 bg-red-50'
                      : 'border-gray-300 bg-white'
                  } ${isLoading ? 'opacity-60 cursor-not-allowed' : ''}`}
                  placeholder={t('auth:register.passwordPlaceholder')}
                />
                {validationErrors.password && (
                  <p className="mt-1 text-sm text-red-600">{validationErrors.password}</p>
                )}
              </div>
              <div>
                <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-700 mb-1">
                  {t('auth:register.confirmPasswordLabel')}
                </label>
                <input
                  id="confirmPassword"
                  name="confirmPassword"
                  type="password"
                  autoComplete="new-password"
                  value={formData.confirmPassword}
                  onChange={handleChange}
                  disabled={isLoading}
                  className={`w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-colors ${
                    validationErrors.confirmPassword
                      ? 'border-red-500 bg-red-50'
                      : 'border-gray-300 bg-white'
                  } ${isLoading ? 'opacity-60 cursor-not-allowed' : ''}`}
                  placeholder={t('auth:register.confirmPasswordPlaceholder')}
                />
                {validationErrors.confirmPassword && (
                  <p className="mt-1 text-sm text-red-600">{validationErrors.confirmPassword}</p>
                )}
              </div>
            </div>

            {/* Terms Checkbox */}
            <div className="flex items-start">
              <input
                id="agreeToTerms"
                name="agreeToTerms"
                type="checkbox"
                checked={formData.agreeToTerms}
                onChange={handleChange}
                disabled={isLoading}
                className="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-gray-300 rounded cursor-pointer mt-1"
              />
              <label htmlFor="agreeToTerms" className="ml-2 block text-sm text-gray-700">
                {t('auth:register.agreeToTermsStart')}{' '}
                <Link to="/terms" className="text-indigo-600 hover:text-indigo-700 font-medium">
                  {t('auth:register.termsOfService')}
                </Link>{' '}
                {t('auth:register.and')}{' '}
                <Link to="/privacy" className="text-indigo-600 hover:text-indigo-700 font-medium">
                  {t('auth:register.privacyPolicy')}
                </Link>
              </label>
            </div>
            {validationErrors.agreeToTerms && (
              <p className="text-sm text-red-600">{validationErrors.agreeToTerms}</p>
            )}

            {/* Submit Button */}
            <button
              type="submit"
              disabled={isLoading}
              className="w-full bg-indigo-600 text-white font-medium py-2 px-4 rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 transition-colors disabled:opacity-60 disabled:cursor-not-allowed mt-6"
            >
              {isLoading ? t('auth:register.creatingAccount') : t('auth:register.createAccount')}
            </button>
          </form>

          {/* Login Link */}
          <div className="mt-6 text-center">
            <p className="text-gray-600">
              {t('auth:register.hasAccount')}{' '}
              <Link to="/login" className="text-indigo-600 hover:text-indigo-700 font-medium">
                {t('auth:register.signInHere')}
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Register;