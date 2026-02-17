import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { EyeIcon, EyeSlashIcon, DevicePhoneMobileIcon, EnvelopeIcon } from '@heroicons/react/24/outline';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from '../../store/authStore';
import { useToast } from '../../contexts/ToastContext';
import { apiService } from '../../services/api';
import HohemaLogo from '../../assets/hohema-logo.png';
import { LanguageSwitcher } from '../../components/LanguageSwitcher';

type LoginMethod = 'email' | 'mobile';

const Login: React.FC = () => {
  const { t } = useTranslation(['auth']);
  const navigate = useNavigate();
  const { login, setLoading, setError, error, isLoading } = useAuthStore();
  const { success, error: showError } = useToast();
  
  const [loginMethod, setLoginMethod] = useState<LoginMethod>('email');
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    phoneNumber: '',
    pin: '',
  });
  const [showPassword, setShowPassword] = useState(false);
  const [validationErrors, setValidationErrors] = useState<{ [key: string]: string }>({});
  const [pinSent, setPinSent] = useState(false);
  const [countdown, setCountdown] = useState(0);

  // Countdown timer effect
  React.useEffect(() => {
    if (countdown > 0) {
      const timer = setTimeout(() => setCountdown(countdown - 1), 1000);
      return () => clearTimeout(timer);
    }
  }, [countdown]);

  const validateForm = () => {
    const errors: { [key: string]: string } = {};
    
    if (loginMethod === 'email') {
      if (!formData.email) {
        errors.email = t('auth:login.validation.emailRequired');
      } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
        errors.email = t('auth:login.validation.emailInvalid');
      }
      
      if (!formData.password) {
        errors.password = t('auth:login.validation.passwordRequired');
      } else if (formData.password.length < 6) {
        errors.password = t('auth:login.validation.passwordMin');
      }
    } else {
      if (!formData.phoneNumber) {
        errors.phoneNumber = t('auth:login.validation.phoneRequired');
      } else if (!/^\+?[1-9]\d{1,14}$/.test(formData.phoneNumber.replace(/\s/g, ''))) {
        errors.phoneNumber = t('auth:login.validation.phoneInvalid');
      }

      if (pinSent && !formData.pin) {
        errors.pin = t('auth:login.validation.pinRequired');
      } else if (pinSent && formData.pin.length !== 6) {
        errors.pin = t('auth:login.validation.pinLength');
      }
    }
    
    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
    // Clear validation error for this field when user starts typing
    if (validationErrors[name]) {
      setValidationErrors(prev => {
        const newErrors = { ...prev };
        delete newErrors[name];
        return newErrors;
      });
    }
  };

  const handleBlur = (e: React.FocusEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    const errors: { [key: string]: string } = {};
    
    if (name === 'email' && value && loginMethod === 'email') {
      if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) {
        errors.email = t('auth:login.validation.emailInvalid');
      }
    }
    
    if (name === 'password' && value && loginMethod === 'email') {
      if (value.length < 6) {
        errors.password = t('auth:login.validation.passwordMin');
      }
    }

    if (name === 'phoneNumber' && value && loginMethod === 'mobile') {
      if (!/^\+?[1-9]\d{1,14}$/.test(value.replace(/\s/g, ''))) {
        errors.phoneNumber = t('auth:login.validation.phoneInvalid');
      }
    }

    if (name === 'pin' && value && loginMethod === 'mobile') {
      if (value.length !== 6) {
        errors.pin = 'PIN must be 6 digits';
      }
    }
    
    if (errors[name]) {
      setValidationErrors(prev => ({ ...prev, ...errors }));
    }
  };

  const handleSendPin = async () => {
    if (!formData.phoneNumber) {
      setValidationErrors({ phoneNumber: 'Phone number is required' });
      return;
    }

    if (!/^\+?[1-9]\d{1,14}$/.test(formData.phoneNumber.replace(/\s/g, ''))) {
      setValidationErrors({ phoneNumber: 'Please enter a valid phone number (e.g., +27812345678)' });
      return;
    }

    setLoading(true);
    setError(null);

    try {
      await apiService.loginMobileRequest({
        phoneNumber: formData.phoneNumber,
      });

      setPinSent(true);
      setCountdown(300); // 5 minutes countdown
      setError(null);
      setValidationErrors({});
      success('PIN sent to your WhatsApp! Check your messages.');
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to send PIN';
      setError(errorMessage);
      showError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }
    
    setLoading(true);
    setError(null);
    
    try {
      if (loginMethod === 'email') {
        const response = await apiService.login({
          email: formData.email,
          password: formData.password,
        });
        
        if (response && response.token && response.user) {
          login(response.user, response.token);
          success(`Welcome back, ${response.user.firstName}!`);
          navigate('/');
        } else {
          setError('Login failed - invalid response from server');
          showError('Login failed - invalid response from server');
        }
      } else {
        // Mobile PIN verification
        const response = await apiService.loginMobileVerify({
          phoneNumber: formData.phoneNumber,
          pin: formData.pin,
        });

        if (response && response.token && response.user) {
          login(response.user, response.token);
          success(`Welcome, ${response.user.firstName}! Logged in via WhatsApp.`);
          navigate('/');
        } else {
          setError('Verification failed - invalid response from server');
          showError('PIN verification failed. Please try again.');
        }
      }
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Login failed';
      setError(errorMessage);
      showError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const switchLoginMethod = (method: LoginMethod) => {
    setLoginMethod(method);
    setPinSent(false);
    setFormData({
      email: '',
      password: '',
      phoneNumber: '',
      pin: '',
    });
    setValidationErrors({});
    setError(null);
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-green-50 to-teal-50 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full relative">
        {/* Language Switcher */}
        <div className="absolute -top-12 right-0">
          <LanguageSwitcher variant="dropdown" showLabel={false} className="" />
        </div>
        
        <div className="bg-white rounded-lg shadow-lg p-8">
          <div className="text-center mb-8">
            <div className="mx-auto flex items-center justify-center">
              <img src={HohemaLogo} alt="Ho Hema Loans" className="h-24 w-auto" />
            </div>
            <h2 className="mt-4 text-3xl font-bold text-gray-900">
              {t('auth:login.title')}
            </h2>
            <p className="mt-2 text-sm text-gray-600">
              {t('auth:login.subtitle')}
            </p>
          </div>

          {error && (
            <div className="mb-4 rounded-md bg-red-50 border border-red-200 p-4">
              <p className="text-sm text-red-700">{error}</p>
            </div>
          )}

          {/* Login Method Toggle */}
          <div className="mb-6 flex gap-2 p-1 bg-gray-100 rounded-lg">
            <button
              type="button"
              onClick={() => switchLoginMethod('email')}
              disabled={isLoading}
              className={`flex-1 py-2 px-4 rounded-md font-medium transition flex items-center justify-center gap-2 ${
                loginMethod === 'email'
                  ? 'bg-white text-blue-600 shadow-sm'
                  : 'text-gray-600 hover:text-gray-900'
              }`}
            >
              <EnvelopeIcon className="h-5 w-5" />
              {t('auth:login.methodEmail')}
            </button>
            <button
              type="button"
              onClick={() => switchLoginMethod('mobile')}
              disabled={isLoading}
              className={`flex-1 py-2 px-4 rounded-md font-medium transition flex items-center justify-center gap-2 ${
                loginMethod === 'mobile'
                  ? 'bg-white text-green-600 shadow-sm'
                  : 'text-gray-600 hover:text-gray-900'
              }`}
            >
              <DevicePhoneMobileIcon className="h-5 w-5" />
              {t('auth:login.methodMobile')}
            </button>
          </div>

          <form className="space-y-5" onSubmit={handleSubmit}>
            {loginMethod === 'email' ? (
              <>
                {/* Email Login Fields */}
                <div>
                  <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                    {t('auth:login.emailLabel')}
                  </label>
                  <input
                    id="email"
                    name="email"
                    type="email"
                    autoComplete="email"
                    disabled={isLoading}
                    className={`w-full px-3 py-2 border rounded-md text-gray-900 placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition ${
                      validationErrors.email ? 'border-red-500 bg-red-50' : 'border-gray-300'
                    }`}
                    placeholder={t('auth:login.emailPlaceholder')}
                    value={formData.email}
                    onChange={handleChange}
                    onBlur={handleBlur}
                  />
                  {validationErrors.email && (
                    <p className="mt-1 text-sm text-red-600">{validationErrors.email}</p>
                  )}
                </div>

                <div>
                  <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
                    {t('auth:login.passwordLabel')}
                  </label>
                  <div className="relative">
                    <input
                      id="password"
                      name="password"
                      type={showPassword ? 'text' : 'password'}
                      autoComplete="current-password"
                      disabled={isLoading}
                      className={`w-full px-3 py-2 border rounded-md text-gray-900 placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition pr-10 ${
                        validationErrors.password ? 'border-red-500 bg-red-50' : 'border-gray-300'
                      }`}
                      placeholder={t('auth:login.passwordPlaceholder')}
                      value={formData.password}
                      onChange={handleChange}
                      onBlur={handleBlur}
                    />
                    <button
                      type="button"
                      disabled={isLoading}
                      className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 hover:text-gray-700 disabled:opacity-50"
                      onClick={() => setShowPassword(!showPassword)}
                    >
                      {showPassword ? (
                        <EyeSlashIcon className="h-5 w-5" />
                      ) : (
                        <EyeIcon className="h-5 w-5" />
                      )}
                    </button>
                  </div>
                  {validationErrors.password && (
                    <p className="mt-1 text-sm text-red-600">{validationErrors.password}</p>
                  )}
                </div>

                <div className="flex items-center justify-between text-sm">
                  <Link to="/forgot-password" className="font-medium text-blue-600 hover:text-blue-500">
                    {t('auth:login.forgotPassword')}
                  </Link>
                </div>
              </>
            ) : (
              <>
                {/* Mobile Login Fields */}
                <div>
                  <label htmlFor="phoneNumber" className="block text-sm font-medium text-gray-700 mb-1">
                    {t('auth:login.mobileLabel')}
                  </label>
                  <input
                    id="phoneNumber"
                    name="phoneNumber"
                    type="tel"
                    autoComplete="tel"
                    disabled={isLoading || pinSent}
                    className={`w-full px-3 py-2 border rounded-md text-gray-900 placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent transition ${
                      validationErrors.phoneNumber ? 'border-red-500 bg-red-50' : 'border-gray-300'
                    }`}
                    placeholder={t('auth:login.mobilePlaceholder')}
                    value={formData.phoneNumber}
                    onChange={handleChange}
                    onBlur={handleBlur}
                  />
                  {validationErrors.phoneNumber && (
                    <p className="mt-1 text-sm text-red-600">{validationErrors.phoneNumber}</p>
                  )}
                  {!pinSent && (
                    <p className="mt-1 text-xs text-gray-500">
                      {t('auth:login.mobileHint')}
                    </p>
                  )}
                </div>

                {!pinSent ? (
                  <button
                    type="button"
                    onClick={handleSendPin}
                    disabled={isLoading || countdown > 0}
                    className="w-full bg-green-600 text-white py-2 px-4 rounded-md font-medium hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition flex items-center justify-center gap-2"
                  >
                    {isLoading && (
                      <svg className="animate-spin h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                      </svg>
                    )}
                    <DevicePhoneMobileIcon className="h-5 w-5" />
                    {countdown > 0 ? t('auth:login.resendIn', { seconds: countdown }) : t('auth:login.sendPin')}
                  </button>
                ) : (
                  <>
                    <div className="bg-green-50 border border-green-200 rounded-md p-3">
                      <p className="text-sm text-green-700 flex items-center gap-2">
                        <svg className="h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
                          <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                        </svg>
                        {t('auth:login.pinSent')}
                      </p>
                    </div>

                    <div>
                      <label htmlFor="pin" className="block text-sm font-medium text-gray-700 mb-1">
                        {t('auth:login.pinLabel')}
                      </label>
                      <input
                        id="pin"
                        name="pin"
                        type="text"
                        inputMode="numeric"
                        pattern="[0-9]*"
                        maxLength={6}
                        disabled={isLoading}
                        className={`w-full px-3 py-2 border rounded-md text-gray-900 placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent transition text-center text-2xl tracking-widest ${
                          validationErrors.pin ? 'border-red-500 bg-red-50' : 'border-gray-300'
                        }`}
                        placeholder={t('auth:login.pinPlaceholder')}
                        value={formData.pin}
                        onChange={handleChange}
                        onBlur={handleBlur}
                      />
                      {validationErrors.pin && (
                        <p className="mt-1 text-sm text-red-600">{validationErrors.pin}</p>
                      )}
                      {countdown > 0 && (
                        <p className="mt-1 text-xs text-gray-500">
                          {t('auth:login.pinExpires', { minutes: Math.floor(countdown / 60), seconds: (countdown % 60).toString().padStart(2, '0') })}
                        </p>
                      )}
                    </div>

                    <button
                      type="button"
                      onClick={() => {
                        setPinSent(false);
                        setFormData(prev => ({ ...prev, pin: '' }));
                      }}
                      disabled={isLoading}
                      className="w-full text-sm text-gray-600 hover:text-gray-900 py-2"
                    >
                      {t('auth:login.changeMobile')}
                    </button>
                  </>
                )}
              </>
            )}

            {(loginMethod === 'email' || pinSent) && (
              <button
                type="submit"
                disabled={isLoading}
                className={`w-full py-2 px-4 rounded-md font-medium focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition flex items-center justify-center ${
                  loginMethod === 'email'
                    ? 'bg-gradient-to-r from-blue-600 to-teal-600 text-white hover:from-blue-700 hover:to-teal-700 focus:ring-blue-500'
                    : 'bg-green-600 text-white hover:bg-green-700 focus:ring-green-500'
                }`}
              >
                {isLoading && (
                  <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                )}
                {isLoading 
                  ? (loginMethod === 'email' ? t('auth:login.signingIn') : t('auth:login.verifying')) 
                  : (loginMethod === 'email' ? t('auth:login.signIn') : t('auth:login.verifyAndSignIn'))}
              </button>
            )}
          </form>

          <div className="mt-6 text-center">
            <p className="text-sm text-gray-600">
              {t('auth:login.noAccount')}{' '}
              <Link to="/register" className="font-medium text-blue-600 hover:text-blue-500">
                {t('auth:login.registerHere')}
              </Link>
            </p>
          </div>

          <div className="mt-4 pt-4 border-t border-gray-200">
            <p className="text-xs text-gray-500 text-center">
              {t('auth:login.agreeTerms')}{' '}
              <Link to="/terms" className="text-blue-600 hover:underline">{t('auth:login.termsOfService')}</Link>
              {' '}{t('auth:login.and')}{' '}
              <Link to="/privacy" className="text-blue-600 hover:underline">{t('auth:login.privacyPolicy')}</Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Login;
