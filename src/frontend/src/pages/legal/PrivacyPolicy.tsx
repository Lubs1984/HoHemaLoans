import React from 'react';
import { Link } from 'react-router-dom';
import HohemaLogo from '../../assets/hohema-logo.png';

const PrivacyPolicy: React.FC = () => {
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
          <h1 className="text-3xl font-bold text-gray-900 mb-2">Privacy Policy</h1>
          <p className="text-sm text-gray-500 mb-8">Last updated: 10 February 2026</p>

          <div className="prose prose-gray max-w-none space-y-6">
            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">1. Introduction</h2>
              <p className="text-gray-700 leading-relaxed">
                Ho Hema Loans (Pty) Ltd ("Ho Hema Loans", "we", "us", or "our") is committed to protecting 
                your personal information and your right to privacy. This Privacy Policy explains how we collect, 
                use, disclose, and safeguard your information when you use our lending platform, website, 
                WhatsApp service, and related services (collectively, the "Platform").
              </p>
              <p className="text-gray-700 leading-relaxed">
                This policy is compliant with the <strong>Protection of Personal Information Act 4 of 2013 (POPIA)</strong> and 
                the <strong>National Credit Act 34 of 2005 (NCA)</strong> of the Republic of South Africa.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">2. Information We Collect</h2>
              <p className="text-gray-700 leading-relaxed mb-3">We collect personal information that you voluntarily provide to us, including but not limited to:</p>
              
              <h3 className="text-lg font-medium text-gray-800 mt-4 mb-2">2.1 Personal Identification Information</h3>
              <ul className="list-disc list-inside text-gray-700 space-y-1 ml-4">
                <li>Full name, surname, and identity number (SA ID)</li>
                <li>Date of birth and gender</li>
                <li>Contact details (email address, phone number, WhatsApp number)</li>
                <li>Residential and postal address</li>
                <li>Marital status and number of dependants</li>
              </ul>

              <h3 className="text-lg font-medium text-gray-800 mt-4 mb-2">2.2 Financial Information</h3>
              <ul className="list-disc list-inside text-gray-700 space-y-1 ml-4">
                <li>Employment details (employer name, occupation, employment status)</li>
                <li>Income information (gross income, net income, other income sources)</li>
                <li>Monthly expenses and existing financial obligations</li>
                <li>Banking details (bank name, account number, branch code)</li>
                <li>Credit history and credit bureau information</li>
              </ul>

              <h3 className="text-lg font-medium text-gray-800 mt-4 mb-2">2.3 Documents</h3>
              <ul className="list-disc list-inside text-gray-700 space-y-1 ml-4">
                <li>Copy of South African ID document or Smart ID card</li>
                <li>Proof of income (payslips, bank statements)</li>
                <li>Proof of residence</li>
                <li>Any additional supporting documents</li>
              </ul>

              <h3 className="text-lg font-medium text-gray-800 mt-4 mb-2">2.4 Technical Information</h3>
              <ul className="list-disc list-inside text-gray-700 space-y-1 ml-4">
                <li>IP address and browser type</li>
                <li>Device information</li>
                <li>Usage data and interaction logs</li>
                <li>WhatsApp message metadata (timestamps, message status)</li>
              </ul>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">3. How We Use Your Information</h2>
              <p className="text-gray-700 leading-relaxed mb-3">We use the information we collect for the following purposes:</p>
              <ul className="list-disc list-inside text-gray-700 space-y-2 ml-4">
                <li><strong>Loan Processing:</strong> To assess your loan application, conduct affordability assessments, and make lending decisions in compliance with the NCA.</li>
                <li><strong>Identity Verification:</strong> To verify your identity and prevent fraud.</li>
                <li><strong>Credit Assessment:</strong> To conduct credit checks with registered credit bureaus as required by the NCA.</li>
                <li><strong>Contract Management:</strong> To generate and manage loan agreements, pre-agreement statements, and Form 39 disclosures.</li>
                <li><strong>Communication:</strong> To communicate with you about your loan application, account status, repayment reminders, and customer support via email, SMS, WhatsApp, or phone.</li>
                <li><strong>Legal Compliance:</strong> To comply with our obligations under the NCA, POPIA, FICA, and other applicable legislation.</li>
                <li><strong>NCR Reporting:</strong> To submit mandatory returns and reports to the National Credit Regulator.</li>
                <li><strong>Service Improvement:</strong> To improve our Platform, products, and customer experience.</li>
              </ul>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">4. Legal Basis for Processing</h2>
              <p className="text-gray-700 leading-relaxed mb-3">Under POPIA, we process your personal information based on the following lawful grounds:</p>
              <ul className="list-disc list-inside text-gray-700 space-y-2 ml-4">
                <li><strong>Consent:</strong> You have given us your express consent to process your information for the stated purposes.</li>
                <li><strong>Contractual Obligation:</strong> Processing is necessary to fulfil our credit agreement with you.</li>
                <li><strong>Legal Obligation:</strong> Processing is required to comply with the NCA, FICA, and other laws.</li>
                <li><strong>Legitimate Interest:</strong> Processing is necessary for our legitimate business interests, such as fraud prevention and service improvement.</li>
              </ul>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">5. Sharing of Information</h2>
              <p className="text-gray-700 leading-relaxed mb-3">We may share your personal information with the following third parties:</p>
              <ul className="list-disc list-inside text-gray-700 space-y-2 ml-4">
                <li><strong>Credit Bureaus:</strong> TransUnion, Experian, and other registered credit bureaus for credit checks and payment reporting as required by the NCA.</li>
                <li><strong>National Credit Regulator (NCR):</strong> For mandatory regulatory reporting and compliance.</li>
                <li><strong>Banking Partners:</strong> To process loan disbursements and repayment collections.</li>
                <li><strong>WhatsApp / Meta:</strong> Message content is transmitted via Meta's WhatsApp Business API for communication purposes.</li>
                <li><strong>Service Providers:</strong> Third-party service providers who assist us with IT infrastructure, hosting, and analytics, under strict confidentiality agreements.</li>
                <li><strong>Legal Authorities:</strong> When required by law, court order, or to protect our rights.</li>
              </ul>
              <p className="text-gray-700 leading-relaxed mt-3">
                We will <strong>never sell</strong> your personal information to third parties for marketing purposes.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">6. Data Retention</h2>
              <p className="text-gray-700 leading-relaxed">
                In accordance with the NCA and POPIA, we retain your personal information for the following periods:
              </p>
              <ul className="list-disc list-inside text-gray-700 space-y-2 ml-4 mt-3">
                <li><strong>Active loan records:</strong> For the duration of the credit agreement plus 5 years after settlement.</li>
                <li><strong>Declined applications:</strong> For a minimum of 3 years from the date of application.</li>
                <li><strong>Financial records:</strong> As required by the Companies Act and Tax Administration Act (minimum 5 years).</li>
                <li><strong>WhatsApp communications:</strong> For the duration of the credit agreement plus the retention period.</li>
                <li><strong>Audit logs:</strong> Retained for a minimum of 5 years for compliance and dispute resolution purposes.</li>
              </ul>
              <p className="text-gray-700 leading-relaxed mt-3">
                After the retention period, your personal information will be securely deleted or anonymised.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">7. Data Security</h2>
              <p className="text-gray-700 leading-relaxed">
                We implement appropriate technical and organisational measures to protect your personal information, including:
              </p>
              <ul className="list-disc list-inside text-gray-700 space-y-2 ml-4 mt-3">
                <li>Encryption of data in transit (TLS/SSL) and at rest</li>
                <li>Secure authentication with JWT tokens and password hashing</li>
                <li>Role-based access control for staff</li>
                <li>Regular security assessments and monitoring</li>
                <li>Secure hosting on encrypted cloud infrastructure</li>
                <li>Comprehensive audit logging of all data access and changes</li>
              </ul>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">8. Your Rights Under POPIA</h2>
              <p className="text-gray-700 leading-relaxed mb-3">As a data subject, you have the following rights:</p>
              <ul className="list-disc list-inside text-gray-700 space-y-2 ml-4">
                <li><strong>Right to Access:</strong> Request confirmation of and access to your personal information we hold.</li>
                <li><strong>Right to Correction:</strong> Request correction of inaccurate or incomplete personal information.</li>
                <li><strong>Right to Deletion:</strong> Request deletion of your personal information where it is no longer necessary (subject to legal retention requirements).</li>
                <li><strong>Right to Object:</strong> Object to the processing of your personal information in certain circumstances.</li>
                <li><strong>Right to Data Portability:</strong> Request a copy of your personal information in a structured format.</li>
                <li><strong>Right to Withdraw Consent:</strong> Withdraw your consent at any time (this will not affect the lawfulness of processing before withdrawal).</li>
                <li><strong>Right to Lodge a Complaint:</strong> Lodge a complaint with the Information Regulator of South Africa.</li>
              </ul>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">9. Cooling-Off Period</h2>
              <p className="text-gray-700 leading-relaxed">
                In terms of Section 121 of the NCA, you have the right to terminate a credit agreement within 
                <strong> 5 (five) business days</strong> from the date you sign the agreement, without penalty. 
                During the cooling-off period, you may cancel the agreement by notifying us via the Platform, 
                WhatsApp, or email. Any amounts disbursed must be repaid in full.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">10. Cookies and Tracking</h2>
              <p className="text-gray-700 leading-relaxed">
                Our Platform uses essential cookies and local storage to maintain your session and authentication state. 
                We do not use third-party tracking cookies or advertising trackers. Technical data collected is used 
                solely for the operation and security of the Platform.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">11. WhatsApp Communications</h2>
              <p className="text-gray-700 leading-relaxed">
                By interacting with us via WhatsApp, you consent to us receiving and storing your WhatsApp messages, 
                phone number, and display name. WhatsApp messages are processed through Meta's WhatsApp Business API 
                and are subject to <a href="https://www.whatsapp.com/legal/privacy-policy" target="_blank" rel="noopener noreferrer" className="text-primary-600 hover:text-primary-700 underline">WhatsApp's Privacy Policy</a>. 
                We store message content for record-keeping and to facilitate your loan application process.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">12. Changes to This Policy</h2>
              <p className="text-gray-700 leading-relaxed">
                We may update this Privacy Policy from time to time. We will notify you of material changes by 
                posting the new Privacy Policy on the Platform with an updated "Last updated" date. We encourage 
                you to review this policy periodically.
              </p>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">13. Information Regulator</h2>
              <p className="text-gray-700 leading-relaxed mb-3">
                If you are not satisfied with how we handle your personal information, you may lodge a complaint with:
              </p>
              <div className="bg-gray-50 rounded-lg p-4 border border-gray-200">
                <p className="text-gray-700 font-medium">The Information Regulator (South Africa)</p>
                <p className="text-gray-600 text-sm mt-1">
                  Email: <a href="mailto:complaints.IR@justice.gov.za" className="text-primary-600 hover:underline">complaints.IR@justice.gov.za</a><br />
                  Website: <a href="https://www.justice.gov.za/inforeg/" target="_blank" rel="noopener noreferrer" className="text-primary-600 hover:underline">www.justice.gov.za/inforeg</a><br />
                  Tel: 012 406 4818
                </p>
              </div>
            </section>

            <section>
              <h2 className="text-xl font-semibold text-gray-900 mt-8 mb-3">14. Contact Us</h2>
              <p className="text-gray-700 leading-relaxed mb-3">
                For any questions or requests regarding this Privacy Policy or your personal information, please contact our Information Officer:
              </p>
              <div className="bg-primary-50 rounded-lg p-4 border border-primary-200">
                <p className="text-gray-800 font-medium">Ho Hema Loans (Pty) Ltd</p>
                <p className="text-gray-700 text-sm mt-1">
                  Information Officer<br />
                  Email: <a href="mailto:privacy@hohemaloans.com" className="text-primary-600 hover:underline">privacy@hohemaloans.com</a><br />
                  WhatsApp: Contact us via the Platform<br />
                  Website: <a href="https://hohemaloans.com" className="text-primary-600 hover:underline">hohemaloans.com</a>
                </p>
              </div>
            </section>
          </div>
        </div>

        {/* Footer links */}
        <div className="mt-8 text-center text-sm text-gray-500">
          <Link to="/terms" className="text-primary-600 hover:text-primary-700">Terms of Service</Link>
          <span className="mx-2">·</span>
          <Link to="/" className="text-primary-600 hover:text-primary-700">Back to Ho Hema Loans</Link>
        </div>
      </main>
    </div>
  );
};

export default PrivacyPolicy;
