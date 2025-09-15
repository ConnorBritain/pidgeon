'use client';

import { useState } from 'react';
import { Workspace } from '@/components/layout';
import { ControlBar, MessageEditor, ValidationPanel, EmptyState, UserMode } from '@/components/workbench';
import { EditorHeader } from '@/components/workbench/EditorHeader';
import { buildGenerateCli, buildValidateCli } from '@/lib/cliMapping';
import { LibraryPanel, VendorPanel, WorkflowsPanel, DiffPanel, DatasetsPanel, ExplorerPanel } from '@/components/leftpanels';

type ValidationMode = 'strict' | 'compatibility';

interface ValidationIssue {
  ruleId: string;
  message: string;
  severity: 'Error' | 'Warning' | 'Info';
  location?: string;
}

interface ValidationResultView {
  isValid: boolean;
  issues: ValidationIssue[];
}

export default function Home() {
  const [aiPanelOpen, setAIPanelOpen] = useState(false);
  const [mode, setMode] = useState<UserMode>('basic');
  const [standard, setStandard] = useState<string>('HL7 v2.x');
  const [messageType, setMessageType] = useState<string>('ADT^A01');
  const [message, setMessage] = useState<string>('');
  const [validationMode, setValidationMode] = useState<ValidationMode>('strict');
  const [validation, setValidation] = useState<ValidationResultView | null>(null);
  const [vendorProfile, setVendorProfile] = useState<string | null>(null);

  const handleGenerate = () => {
    if (standard.toLowerCase().includes('hl7')) {
      const now = new Date().toISOString();
      setMessage(`MSH|^~\\&|PIDGEON|FAC|RCV|DEST|${now}|PIDGEON|ADT^A01|123|P|2.3\nPID|1||12345^^^HOSP^MR||DOE^JOHN^^^^^L|`);
    } else if (standard.toLowerCase().includes('fhir')) {
      setMessage(`{\n  "resourceType": "Patient",\n  "id": "example",\n  "name": [{ "family": "Doe", "given": ["John"] }]\n}`);
    } else {
      setMessage('NewRx|example-stub');
    }
    // Auto-clear validation result
    setValidation(null);
  };

  const handleValidate = () => {
    if (message.trim().length === 0) {
      setValidation({ isValid: false, issues: [{ ruleId: 'EMPTY', message: 'No message content', severity: 'Error' }] });
      return;
    }
    if (standard.toLowerCase().includes('hl7')) {
      const isHL7 = message.startsWith('MSH');
      const issues: ValidationIssue[] = [];
      if (!isHL7) {
        issues.push({ ruleId: 'HL7_MSH_REQUIRED', message: 'MSH segment missing', severity: 'Error' });
      }
      setValidation({ isValid: issues.length === 0, issues });
    } else if (standard.toLowerCase().includes('fhir')) {
      try {
        JSON.parse(message);
        setValidation({ isValid: true, issues: [] });
      } catch {
        setValidation({ isValid: false, issues: [{ ruleId: 'JSON_PARSE', message: 'Invalid JSON', severity: 'Error' }] });
      }
    } else {
      setValidation({ isValid: true, issues: [] });
    }
  };

  const handleImport = (deidentify: boolean) => {
    // Stub: simulate import
    const imported = 'MSH|^~\\&|...\nPID|...';
    setMessage(imported + (deidentify ? '\n# De-identified preview' : ''));
    setValidation(null);
  };

  const handleAnalyzeVendor = () => {
    // Stub: pretend we saved a vendor profile
    setVendorProfile('epic_er.json');
  };

  const handleUseVendor = () => {
    if (!vendorProfile) setVendorProfile('epic_er.json');
  };

  const genCli = () => buildGenerateCli(standard, messageType, 1, vendorProfile);
  const valCli = () => buildValidateCli('current.hl7', validationMode);

  const renderLeftPanel = (active: string) => {
    switch (active) {
      case 'explorer':
        return <ExplorerPanel />;
      case 'library':
        return <LibraryPanel />;
      case 'vendor':
        return <VendorPanel />;
      case 'workflows':
        return <WorkflowsPanel />;
      case 'diff':
        return <DiffPanel />;
      case 'datasets':
        return <DatasetsPanel />;
      default:
        return null;
    }
  };

  return (
    <Workspace
      aiPanelOpen={aiPanelOpen}
      onAIPanelToggle={() => setAIPanelOpen(!aiPanelOpen)}
      renderLeftPanel={renderLeftPanel}
    >
      <div className="p-desktop-md space-y-desktop-md max-w-6xl mx-auto w-full">
        <ControlBar
          mode={mode}
          onModeChange={setMode}
          standard={standard}
          onStandardChange={(s) => {
            setStandard(s);
            // Adjust default types when switching standards
            if (s.toLowerCase().includes('hl7')) setMessageType('ADT^A01');
            else if (s.toLowerCase().includes('fhir')) setMessageType('Patient');
            else setMessageType('NewRx');
          }}
          messageType={messageType}
          onMessageTypeChange={setMessageType}
          hasMessage={message.trim().length > 0}
          onGenerate={handleGenerate}
          onValidate={handleValidate}
          onImport={handleImport}
          aiOpen={aiPanelOpen}
          onToggleAI={() => setAIPanelOpen(!aiPanelOpen)}
          vendorProfile={vendorProfile}
          onAnalyzeVendor={handleAnalyzeVendor}
          onUseVendor={handleUseVendor}
          buildGenerateCli={genCli}
          buildValidateCli={valCli}
        />

        {message.trim().length === 0 ? (
          <EmptyState
            onQuickGenerate={handleGenerate}
            onImportDeidentify={() => handleImport(true)}
            onPickTemplate={(std, type) => {
              setStandard(std);
              setMessageType(type);
            }}
            primaryLabel={`Generate ${standard.includes('HL7') ? messageType : messageType}`}
          />
        ) : (
          <>
            <EditorHeader
              label={`${standard} • ${messageType}`}
              onValidate={handleValidate}
              buildValidateCli={valCli}
            />
            <MessageEditor value={message} onChange={setMessage} />
            <ValidationPanel result={validation} buildValidateCli={valCli} />
          </>
        )}
      </div>
    </Workspace>
  );
}
