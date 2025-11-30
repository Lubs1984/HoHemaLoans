import React, { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { apiService } from '../../services/api';
import Step0_LoanAmount from './steps/Step0_LoanAmount.tsx';
import Step1_TermMonths from './steps/Step1_TermMonths.tsx';
import Step2_Purpose from './steps/Step2_Purpose.tsx';
import Step3_AffordabilityReview from './steps/Step3_AffordabilityReview.tsx';
import Step4_PreviewTerms from './steps/Step4_PreviewTerms.tsx';
import Step5_BankDetails from './steps/Step5_BankDetails.tsx';
import Step6_DigitalSignature from './steps/Step6_DigitalSignature.tsx';

interface LoanApplicationData {
  id?: string;
  amount: number;
  termMonths: number;
  purpose: string;
  purposeDescription?: string;
  bankName: string;
  accountNumber: string;
  accountHolderName: string;
  currentStep: number;
  interestRate?: number;
  monthlyPayment?: number;
  totalAmount?: number;
  otp?: string;
}

const STEPS = [
  { number: 0, title: 'Loan Amount', description: 'How much do you need?' },
  { number: 1, title: 'Repayment Term', description: 'Select repayment period' },
  { number: 2, title: 'Loan Purpose', description: 'What is the loan for?' },
  { number: 3, title: 'Affordability Check', description: 'Can you afford this loan?' },
  { number: 4, title: 'Review Terms', description: 'Review your loan details' },
  { number: 5, title: 'Bank Details', description: 'Where should we send the money?' },
  { number: 6, title: 'Digital Signature', description: 'Finalize your application' },
];

const LoanApplicationWizard: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [currentStep, setCurrentStep] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [applicationData, setApplicationData] = useState<LoanApplicationData>({
    amount: 5000,
    termMonths: 12,
    purpose: '',
    purposeDescription: '',
    bankName: '',
    accountNumber: '',
    accountHolderName: '',
    currentStep: 0,
  });

  // Load existing draft application if ID is provided, or resume from another channel
  useEffect(() => {
    const draftId = searchParams.get('id');
    const shouldResume = searchParams.get('resume') === 'true';
    
    if (draftId) {
      loadDraftApplication(draftId);
    } else if (shouldResume) {
      resumeApplication();
    } else {
      createDraftApplication();
    }
  }, [searchParams]);

  const createDraftApplication = async () => {
    try {
      setLoading(true);
      const response = await apiService.request<any>('/loanapplications/draft', {
        method: 'POST',
        body: JSON.stringify({ channelOrigin: 'Web' }),
      });
      setApplicationData((prev) => ({ ...prev, id: response.id }));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create draft application');
    } finally {
      setLoading(false);
    }
  };

  const loadDraftApplication = async (id: string) => {
    try {
      setLoading(true);
      const response = await apiService.request<any>(`/loanapplications/${id}`);
      setApplicationData({
        id: response.id,
        amount: response.amount || 5000,
        termMonths: response.termMonths || 12,
        purpose: response.purpose || '',
        bankName: response.bankName || '',
        accountNumber: response.accountNumber || '',
        accountHolderName: response.accountHolderName || '',
        currentStep: response.currentStep || 0,
        interestRate: response.interestRate,
        monthlyPayment: response.monthlyPayment,
        totalAmount: response.totalAmount,
      });
      setCurrentStep(response.currentStep || 0);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load draft application');
    } finally {
      setLoading(false);
    }
  };

  const resumeApplication = async () => {
    try {
      setLoading(true);
      const response = await apiService.request<any>('/loanapplications/resume', {
        method: 'POST',
        body: JSON.stringify({ targetChannel: 'Web' }),
      });
      
      if (response) {
        setApplicationData({
          id: response.id,
          amount: response.amount || 5000,
          termMonths: response.termMonths || 12,
          purpose: response.purpose || '',
          bankName: response.bankName || '',
          accountNumber: response.accountNumber || '',
          accountHolderName: response.accountHolderName || '',
          currentStep: response.currentStep || 0,
          interestRate: response.interestRate,
          monthlyPayment: response.monthlyPayment,
          totalAmount: response.totalAmount,
        });
        setCurrentStep(response.currentStep || 0);
      } else {
        // No draft found, create new
        await createDraftApplication();
      }
    } catch (err) {
      // If no draft found, create new one
      await createDraftApplication();
    } finally {
      setLoading(false);
    }
  };
      setLoading(false);
    }
  };

  const saveStepData = async (stepNumber: number, data: Partial<LoanApplicationData>) => {
    if (!applicationData.id) return;

    try {
      setLoading(true);
      const response = await apiService.request<any>(
        `/loanapplications/${applicationData.id}/step/${stepNumber}`,
        {
          method: 'PUT',
          body: JSON.stringify({
            amount: data.amount,
            termMonths: data.termMonths,
            purpose: data.purpose,
            bankName: data.bankName,
            accountNumber: data.accountNumber,
            accountHolderName: data.accountHolderName,
          }),
        }
      );

      // Update local state with response (includes calculated fields)
      setApplicationData((prev) => ({
        ...prev,
        ...data,
        interestRate: response.interestRate || prev.interestRate,
        monthlyPayment: response.monthlyPayment || prev.monthlyPayment,
        totalAmount: response.totalAmount || prev.totalAmount,
      }));
    } catch (err) {
      throw new Error(err instanceof Error ? err.message : 'Failed to save step data');
    } finally {
      setLoading(false);
    }
  };

  const nextStep = async (data: Partial<LoanApplicationData>) => {
    try {
      setError(null);
      await saveStepData(currentStep, data);
      setApplicationData((prev) => ({ ...prev, ...data }));
      setCurrentStep((prev) => Math.min(prev + 1, STEPS.length - 1));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    }
  };

  const prevStep = () => {
    setCurrentStep((prev) => Math.max(prev - 1, 0));
  };

  const submitApplication = async (data: Partial<LoanApplicationData>) => {
    if (!applicationData.id) return;

    try {
      setError(null);
      setLoading(true);
      await apiService.request(`/loanapplications/${applicationData.id}/submit`, {
        method: 'POST',
        body: JSON.stringify({ otp: data.otp || '' }),
      });
      navigate('/loans', { state: { message: 'Application submitted successfully!' } });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to submit application');
    } finally {
      setLoading(false);
    }
  };

  const handleContinueOnWhatsApp = () => {
    if (!applicationData.id) return;
    
    // TODO: Replace with actual WhatsApp business number
    const whatsappNumber = '27822531234'; // Placeholder
    const message = `RESUME ${applicationData.id}`;
    const whatsappUrl = `https://wa.me/${whatsappNumber}?text=${encodeURIComponent(message)}`;
    
    window.open(whatsappUrl, '_blank');
  };

  const renderStep = () => {
    const stepProps = {
      data: applicationData,
      onNext: nextStep,
      onPrev: prevStep,
      loading,
    };

    switch (currentStep) {
      case 0:
        return <Step0_LoanAmount {...stepProps} />;
      case 1:
        return <Step1_TermMonths {...stepProps} />;
      case 2:
        return <Step2_Purpose {...stepProps} />;
      case 3:
        return <Step3_AffordabilityReview {...stepProps} />;
      case 4:
        return <Step4_PreviewTerms {...stepProps} />;
      case 5:
        return <Step5_BankDetails {...stepProps} />;
      case 6:
        return <Step6_DigitalSignature {...stepProps} onSubmit={submitApplication} />;
      default:
        return null;
    }
  };

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Progress Indicator */}
      <div className="bg-white rounded-lg shadow-sm p-6">
        <div className="flex items-center justify-between mb-4">
          {STEPS.map((step, index) => (
            <div key={step.number} className="flex items-center flex-1">
              <div className="flex flex-col items-center flex-1">
                <div
                  className={`w-10 h-10 rounded-full flex items-center justify-center font-semibold transition ${
                    index < currentStep
                      ? 'bg-green-500 text-white'
                      : index === currentStep
                      ? 'bg-blue-600 text-white'
                      : 'bg-gray-200 text-gray-600'
                  }`}
                >
                  {index < currentStep ? '✓' : index + 1}
                </div>
                <div className="text-center mt-2">
                  <p
                    className={`text-xs font-medium ${
                      index === currentStep ? 'text-blue-600' : 'text-gray-600'
                    }`}
                  >
                    {step.title}
                  </p>
                </div>
              </div>
              {index < STEPS.length - 1 && (
                <div
                  className={`h-1 flex-1 mx-2 transition ${
                    index < currentStep ? 'bg-green-500' : 'bg-gray-200'
                  }`}
                />
              )}
            </div>
          ))}
        </div>
        <div className="text-center">
          <h2 className="text-2xl font-bold text-gray-900">{STEPS[currentStep].title}</h2>
          <p className="text-gray-600 mt-1">{STEPS[currentStep].description}</p>
        </div>
      </div>

      {/* Error Message */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-700">{error}</p>
        </div>
      )}

      {/* Continue on WhatsApp Button */}
      {applicationData.id && currentStep < 6 && (
        <div className="bg-green-50 border border-green-200 rounded-lg p-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-3">
              <span className="text-3xl">📱</span>
              <div>
                <h3 className="font-semibold text-gray-900">Continue on WhatsApp</h3>
                <p className="text-sm text-gray-600">Switch to WhatsApp to complete your application on the go</p>
              </div>
            </div>
            <button
              onClick={handleContinueOnWhatsApp}
              className="bg-green-600 hover:bg-green-700 text-white px-6 py-3 rounded-lg font-medium shadow-md hover:shadow-lg transition flex items-center space-x-2"
            >
              <span>Continue on WhatsApp</span>
              <span>→</span>
            </button>
          </div>
        </div>
      )}

      {/* Current Step */}
      <div className="bg-white rounded-lg shadow-sm p-6">{renderStep()}</div>
    </div>
  );
};

export default LoanApplicationWizard;
