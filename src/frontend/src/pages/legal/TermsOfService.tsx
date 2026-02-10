import React from 'react';
import { Link } from 'react-router-dom';
import HohemaLogo from '../../assets/hohema-logo.png';

const TermsOfService: React.FC = () => {
  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white border-b border-gray-200 sticky top-0 z-10">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex items-center justify-between">
          <Link to="/" className="flex items-center gap-3">
            <img className="h-10 w-auto" src={HohemaLogo} alt="Ho Hema Loans" />
            <span className="text-lg font-semibold text-gray-900">Ho Hema Loans</span>
          </Link>
          <Link to="/" className="text-sm text-primary-600 hover:text-primary-700 font-medium">
            ← Back to App
          </Link>
        </div>
      </header>

      {/* Content */}
      <main className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-8 sm:p-12">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">Terms of Service</h1>
          <p className="text-sm text-gray-500 mb-8">Last updated: 10 February 2026</p>

          <div className="prose prose-gray max-w-none space-y-6">
            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">1. Introduction and Acceptance</h2>
              <p className="text-gray-700 leading-relaxed">
                These Terms of Service ("Terms") govern your use of the Ho Hema Loans platform, including our
                website, mobile interface, WhatsApp service, and all related services (the "Platform"), 
                operated by Ho Hema Loans (Pty) Ltd ("Ho Hema Loans", "we", "us", or "our"), 
                a credit provider registered under the National Credit Act 34 of 2005 ("NCA").
              </p>
              <p className="text-gray-700 leading-relaxed mt-3">
                By accessing or using the Platform, you agree to be bound by these Terms. If you do not agree 
                to these Terms, you must not use the Platform. These Terms constitute a legally binding agreement 
                between you and Ho Hema Loans.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">2. Eligibility</h2>
              <p className="text-gray-700 leading-relaxed mb-3">To use our Platform and apply for a loan, you must:</p>
              <ul className="list-disc list-inside text-gray-700 space-y-2 ml-4">
                <li>Be at least <strong>18 years of age</strong></li>
                <li>Be a <strong>South African citizen or permanent resident</strong> with a valid SA ID number</li>
                <li>Have a valid South African <strong>bank account</strong> in your own name</li>
                <li>Have a regular source of <strong>verifiable income</strong></li>
                <li>Not be under <strong>debt review, administration, or sequestration</strong></li>
                <li>Have the <strong>legal capacity</strong> to enter into a credit agreement</li>
              </ul>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">3. Account Registration</h2>
              <p className="text-gray-700 leading-relaxed">
                To access certain features of the Platform, you must create an account. You agree to:
              </p>
              <ul className="list-disc list-inside text-gray-700 space-y-2 ml-4 mt-3">
                <li>Provide <strong>accurate, current, and complete</strong> information during registration</li>
                <li>Maintain and promptly update your account information</li>
                <li>Keep your password <strong>confidential</strong> and not share your account credentials</li>
                <li>Accept responsibility for <strong>all activities</strong> that occur under your account</li>
                <li>Notify us immediately of any <strong>unauthorised access</strong> to your account</li>
              </ul>
              <p className="text-gray-700 leading-relaxed mt-3">
                We reserve the right to suspend or terminate accounts that contain false or misleading information.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">4. Loan Application Process</h2>
              
              <h3 className="text-lg font-medium text-gray-800 mt-4 mb-2">4.1 Application</h3>
              <p className="text-gray-700 leading-relaxed">
                You may apply for a loan through our web portal or WhatsApp service. Submitting a loan application 
                does not guarantee approval. All applications are subject to our assessment criteria, including 
                affordability assessment, credit checks, and document verification, as required by the NCA.
              </p>

              <h3 className="text-lg font-medium text-gray-800 mt-4 mb-2">4.2 Affordability Assessment</h3>
              <p className="text-gray-700 leading-relaxed">
                In compliance with Section 81 of the NCA, we are required to conduct a thorough affordability 
                assessment before granting credit. This includes verifying your income, expenses, and existing 
                debt obligations. The assessment uses a maximum debt-to-income ratio of <strong>35%</strong> 
                as prescribed by the NCR affordability regulations.
              </p>

              <h3 className="text-lg font-medium text-gray-800 mt-4 mb-2">4.3 Credit Bureau Checks</h3>
              <p className="text-gray-700 leading-relaxed">
                By submitting a loan application, you consent to us conducting credit checks with registered 
                South African credit bureaus (including TransUnion and Experian). These checks will be recorded 
                on your credit profile. We also report your payment behaviour to credit bureaus as required by the NCA.
              </p>

              <h3 className="text-lg font-medium text-gray-800 mt-4 mb-2">4.4 Approval and Disbursement</h3>
              <p className="text-gray-700 leading-relaxed">
                Approved loans are subject to the terms set out in the credit agreement (Form 39). Loan funds 
                will be disbursed to the bank account you have provided, typically within 24–48 hours of 
                agreement signing.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">5. Fees and Interest Rates</h2>
              <p className="text-gray-700 leading-relaxed mb-3">
                All fees and interest rates are in compliance with the NCA and NCR guidelines:
              </p>
              <div className="bg-gray-50 rounded-lg p-4 border border-gray-200 mt-3">
                <table className="w-full text-sm">
                  <tbody className="divide-y divide-gray-200">
                    <tr>
                      <td className="py-2 font-medium text-gray-700">Maximum Interest Rate</td>
                      <td className="py-2 text-gray-600">27.5% per annum (as prescribed by the NCA for short-term credit)</td>
                    </tr>
                    <tr>
                      <td className="py-2 font-medium text-gray-700">Initiation Fee</td>
                      <td className="py-2 text-gray-600">Up to R1,140 (VAT inclusive), based on loan amount</td>
                    </tr>
                    <tr>
                      <td className="py-2 font-medium text-gray-700">Monthly Service Fee</td>
                      <td className="py-2 text-gray-600">Up to R60 (VAT inclusive)</td>
                    </tr>
                    <tr>
                      <td className="py-2 font-medium text-gray-700">Credit Life Insurance</td>
                      <td className="py-2 text-gray-600">As applicable, in accordance with NCA regulations</td>
                    </tr>
                  </tbody>
                </table>
              </div>
              <p className="text-gray-700 leading-relaxed mt-3">
                The exact fees and interest rate applicable to your loan will be disclosed in the 
                <strong> Pre-Agreement Statement</strong> and <strong>Credit Agreement (Form 39)</strong> 
                before you sign. Total cost of credit, including all fees and interest, will be clearly stated.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">6. Credit Agreement and Digital Signature</h2>
              <p className="text-gray-700 leading-relaxed">
                Upon loan approval, you will receive a Pre-Agreement Statement and Credit Agreement (Form 39) 
                as required by Section 92 of the NCA. You must review these documents carefully before signing.
              </p>
              <p className="text-gray-700 leading-relaxed mt-3">
                Our Platform uses a <strong>PIN-based digital signature system</strong>. A one-time PIN will be 
                sent to your registered WhatsApp number or phone. By entering this PIN, you confirm that:
              </p>
              <ul className="list-disc list-inside text-gray-700 space-y-2 ml-4 mt-3">
                <li>You have read and understood the credit agreement</li>
                <li>You have received the Pre-Agreement Statement at least 5 business days or waive this right</li>
                <li>You consent to the terms, interest rate, and fees</li>
                <li>Your digital signature is legally binding under the Electronic Communications and Transactions Act 25 of 2002</li>
              </ul>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">7. Cooling-Off Period</h2>
              <p className="text-gray-700 leading-relaxed">
                In accordance with <strong>Section 121 of the NCA</strong>, you have the right to cancel your 
                credit agreement within <strong>5 (five) business days</strong> from the date you sign it, 
                without providing a reason and without penalty.
              </p>
              <p className="text-gray-700 leading-relaxed mt-3">
                To exercise your cooling-off rights, you must:
              </p>
              <ul className="list-disc list-inside text-gray-700 space-y-2 ml-4 mt-3">
                <li>Notify us in writing (via the Platform, WhatsApp, or email) within the 5-day period</li>
                <li>Repay the full disbursed amount plus any interest accrued during the cooling-off period</li>
              </ul>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">8. Repayment Obligations</h2>
              <p className="text-gray-700 leading-relaxed">
                You agree to repay the loan in accordance with the repayment schedule set out in your credit agreement. 
                This includes:
              </p>
              <ul className="list-disc list-inside text-gray-700 space-y-2 ml-4 mt-3">
                <li>Making monthly payments on the agreed dates</li>
                <li>Ensuring sufficient funds are available for deductions</li>
                <li>Notifying us immediately if you anticipate difficulty in making payments</li>
              </ul>
              <p className="text-gray-700 leading-relaxed mt-3">
                <strong>Late Payment:</strong> If you fail to make a payment on the due date, we will contact 
                you to arrange payment. Persistent non-payment may result in legal action and adverse credit 
                bureau reporting. We will always follow the NCA's prescribed procedures before proceeding with 
                any enforcement action.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">9. Early Settlement</h2>
              <p className="text-gray-700 leading-relaxed">
                You have the right to settle your loan early at any time, in accordance with <strong>Section 125 
                of the NCA</strong>. If you settle early, you may be entitled to a proportional reduction in 
                interest charges. An early settlement quote will be provided upon request, valid for 
                <strong> 5 business days</strong>.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">10. WhatsApp Service Terms</h2>
              <p className="text-gray-700 leading-relaxed">
                Our WhatsApp service allows you to apply for loans, check your balance, upload documents, and 
                communicate with us. By using this service:
              </p>
              <ul className="list-disc list-inside text-gray-700 space-y-2 ml-4 mt-3">
                <li>You consent to receiving messages from us via WhatsApp, including application updates, reminders, and notifications</li>
                <li>You acknowledge that WhatsApp messages are transmitted via Meta's infrastructure and are subject to WhatsApp's Terms of Service</li>
                <li>You agree not to send abusive, offensive, or threatening content</li>
                <li>You understand that message delivery depends on your internet connectivity and WhatsApp availability</li>
                <li>You can opt out of WhatsApp communications at any time by sending "STOP"</li>
              </ul>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">11. Consumer Rights Under the NCA</h2>
              <p className="text-gray-700 leading-relaxed mb-3">As a consumer, you have the following rights under the NCA:</p>
              <ul className="list-disc list-inside text-gray-700 space-y-2 ml-4">
                <li><strong>Right to Apply for Credit:</strong> You cannot be unfairly discriminated against when applying for credit.</li>
                <li><strong>Right to Information:</strong> You have the right to receive clear and understandable pre-agreement statements and quotations.</li>
                <li><strong>Right to a Cooling-Off Period:</strong> 5 business days to cancel after signing.</li>
                <li><strong>Right to Early Settlement:</strong> Settle your loan early with applicable rebates.</li>
                <li><strong>Right to a Statement of Account:</strong> Request a statement of your loan account at any time.</li>
                <li><strong>Right to Lodge a Complaint:</strong> File a complaint with us, the NCR, or the relevant ombud.</li>
                <li><strong>Right Against Over-Indebtedness:</strong> Apply for debt review if you are over-indebted.</li>
              </ul>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">12. Complaints Procedure</h2>
              <p className="text-gray-700 leading-relaxed">
                If you have a complaint about our services or your credit agreement, please follow these steps:
              </p>
              <ol className="list-decimal list-inside text-gray-700 space-y-2 ml-4 mt-3">
                <li><strong>Internal Complaint:</strong> Contact us via the Platform, WhatsApp, or email at <a href="mailto:complaints@hohemaloans.com" className="text-primary-600 hover:underline">complaints@hohemaloans.com</a>. We aim to resolve complaints within 14 business days.</li>
                <li><strong>NCR Complaint:</strong> If unresolved, you may escalate to the National Credit Regulator at <a href="mailto:complaints@ncr.org.za" className="text-primary-600 hover:underline">complaints@ncr.org.za</a> or call 0860 627 627.</li>
                <li><strong>Ombud:</strong> You may also refer the matter to the relevant industry ombud for independent resolution.</li>
              </ol>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">13. Prohibited Uses</h2>
              <p className="text-gray-700 leading-relaxed mb-3">You agree not to:</p>
              <ul className="list-disc list-inside text-gray-700 space-y-2 ml-4">
                <li>Provide false, misleading, or fraudulent information</li>
                <li>Use the Platform for any unlawful purpose</li>
                <li>Attempt to gain unauthorised access to the Platform or other users' accounts</li>
                <li>Interfere with or disrupt the Platform's functionality</li>
                <li>Use automated systems (bots, scrapers) to access the Platform</li>
                <li>Impersonate another person or misrepresent your identity</li>
              </ul>
              <p className="text-gray-700 leading-relaxed mt-3">
                Violation of these provisions may result in account termination and legal action.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">14. Intellectual Property</h2>
              <p className="text-gray-700 leading-relaxed">
                All content, design, logos, and technology on the Platform are the property of Ho Hema Loans 
                or its licensors and are protected by South African intellectual property laws. You may not 
                reproduce, distribute, or create derivative works from any Platform content without our 
                prior written consent.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">15. Limitation of Liability</h2>
              <p className="text-gray-700 leading-relaxed">
                To the maximum extent permitted by law:
              </p>
              <ul className="list-disc list-inside text-gray-700 space-y-2 ml-4 mt-3">
                <li>The Platform is provided "as is" without warranties of any kind</li>
                <li>We are not liable for any indirect, incidental, or consequential damages arising from your use of the Platform</li>
                <li>We are not responsible for losses caused by factors beyond our reasonable control (including internet outages, system failures, or third-party service disruptions)</li>
                <li>Our total liability for any claim shall not exceed the total fees paid by you in the 12 months preceding the claim</li>
              </ul>
              <p className="text-gray-700 leading-relaxed mt-3">
                Nothing in these Terms limits or excludes liability that cannot be limited or excluded under applicable South African law, including the Consumer Protection Act 68 of 2008.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">16. Modifications to Terms</h2>
              <p className="text-gray-700 leading-relaxed">
                We reserve the right to modify these Terms at any time. Material changes will be communicated 
                to you via the Platform, email, or WhatsApp at least <strong>20 business days</strong> before 
                they take effect, as required by the NCA. Your continued use of the Platform after changes take 
                effect constitutes acceptance of the modified Terms.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">17. Governing Law and Jurisdiction</h2>
              <p className="text-gray-700 leading-relaxed">
                These Terms are governed by and construed in accordance with the laws of the 
                <strong> Republic of South Africa</strong>. Any disputes arising from these Terms shall be 
                subject to the exclusive jurisdiction of the South African courts.
              </p>
              <p className="text-gray-700 leading-relaxed mt-3">
                Applicable legislation includes, but is not limited to:
              </p>
              <ul className="list-disc list-inside text-gray-700 space-y-1 ml-4 mt-2">
                <li>National Credit Act 34 of 2005 (NCA)</li>
                <li>Protection of Personal Information Act 4 of 2013 (POPIA)</li>
                <li>Consumer Protection Act 68 of 2008 (CPA)</li>
                <li>Electronic Communications and Transactions Act 25 of 2002 (ECTA)</li>
                <li>Financial Intelligence Centre Act 38 of 2001 (FICA)</li>
              </ul>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">18. Severability</h2>
              <p className="text-gray-700 leading-relaxed">
                If any provision of these Terms is found to be invalid or unenforceable, the remaining 
                provisions will continue in full force and effect.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">19. Contact Information</h2>
              <p className="text-gray-700 leading-relaxed mb-3">
                For any questions about these Terms, please contact us:
              </p>
              <div className="bg-primary-50 rounded-lg p-4 border border-primary-200">
                <p className="text-gray-800 font-medium">Ho Hema Loans (Pty) Ltd</p>
                <p className="text-gray-700 text-sm mt-1">
                  General Enquiries: <a href="mailto:info@hohemaloans.com" className="text-primary-600 hover:underline">info@hohemaloans.com</a><br />
                  Complaints: <a href="mailto:complaints@hohemaloans.com" className="text-primary-600 hover:underline">complaints@hohemaloans.com</a><br />
                  WhatsApp: Contact us via the Platform<br />
                  Website: <a href="https://hohemaloans.com" className="text-primary-600 hover:underline">hohemaloans.com</a>
                </p>
              </div>
            </section>

            <section className="mt-8 pt-6 border-t border-gray-200">
              <h2 className="text-xl font-semibold text-gray-900 mb-3">National Credit Regulator</h2>
              <div className="bg-gray-50 rounded-lg p-4 border border-gray-200">
                <p className="text-gray-700 text-sm">
                  <strong>National Credit Regulator (NCR)</strong><br />
                  Tel: 0860 627 627<br />
                  Email: <a href="mailto:complaints@ncr.org.za" className="text-primary-600 hover:underline">complaints@ncr.org.za</a><br />
                  Website: <a href="https://www.ncr.org.za" target="_blank" rel="noopener noreferrer" className="text-primary-600 hover:underline">www.ncr.org.za</a>
                </p>
              </div>
            </section>
          </div>
        </div>

        {/* Footer links */}
        <div className="mt-8 text-center text-sm text-gray-500">
          <Link to="/privacy" className="text-primary-600 hover:text-primary-700">Privacy Policy</Link>
          <span className="mx-2">·</span>
          <Link to="/" className="text-primary-600 hover:text-primary-700">Back to Ho Hema Loans</Link>
        </div>
      </main>
    </div>
  );
};

export default TermsOfService;
