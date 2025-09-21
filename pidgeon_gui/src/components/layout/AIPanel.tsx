'use client';

import { useState, useEffect, useRef } from 'react';
import { cn } from '@/lib/utils';
import { X, MessageCircle, Edit, Search } from 'lucide-react';
import { Button } from '../ui/Button';
import { Card, CardContent } from '../ui/Card';

interface AIPanelProps {
  onClose?: () => void;
}

type AIModeType = 'chat' | 'draft' | 'analyze';

const AI_MODES = [
  {
    id: 'chat' as AIModeType,
    label: 'Chat',
    icon: <MessageCircle size={16} />,
    description: 'Conversational healthcare message assistance'
  },
  {
    id: 'draft' as AIModeType,
    label: 'Draft',
    icon: <Edit size={16} />,
    description: 'Intelligent message composition'
  },
  {
    id: 'analyze' as AIModeType,
    label: 'Analyze', 
    icon: <Search size={16} />,
    description: 'Deep message inspection and compliance'
  }
];

export function AIPanel({ onClose }: AIPanelProps) {
  const [activeMode, setActiveMode] = useState<AIModeType>('chat');
  const [width, setWidth] = useState<number>(380);
  const dragRef = useRef<HTMLDivElement | null>(null);
  const dragging = useRef<boolean>(false);

  useEffect(() => {
    const onMove = (e: MouseEvent) => {
      if (!dragging.current || !dragRef.current) return;
      const rect = dragRef.current.getBoundingClientRect();
      // We measure from the right edge; delta is negative as we move left
      const rightEdge = rect.right;
      const newWidth = Math.min(640, Math.max(320, rightEdge - e.clientX));
      setWidth(newWidth);
    };
    const onUp = () => { dragging.current = false; };
    window.addEventListener('mousemove', onMove);
    window.addEventListener('mouseup', onUp);
    return () => {
      window.removeEventListener('mousemove', onMove);
      window.removeEventListener('mouseup', onUp);
    };
  }, []);

  return (
    <div ref={dragRef} style={{ width }} className="bg-ph-desktop-panel-bg border-l border-ph-desktop-border flex flex-col h-full relative">
      {/* Resizer handle */}
      <div
        role="separator"
        aria-orientation="vertical"
        title="Drag to resize"
        onMouseDown={() => { dragging.current = true; }}
        className="absolute left-0 top-0 bottom-0 w-1 cursor-col-resize hover:bg-ph-gray-800"
      />

      {/* Header */}
      <div className="flex items-center justify-between p-desktop-md border-b border-ph-desktop-border">
        <h2 className="ph-text-desktop-lg font-semibold">Healthcare AI Assistant</h2>
        {onClose && (
          <Button
            variant="secondary"
            size="sm"
            onClick={onClose}
            className="p-1"
            aria-label="Close AI Panel"
          >
            <X size={16} />
          </Button>
        )}
      </div>

      {/* Mode Tabs */}
      <div className="flex border-b border-ph-desktop-border">
        {AI_MODES.map((mode) => (
          <button
            key={mode.id}
            className={cn(
              'flex-1 flex items-center justify-center gap-2 p-desktop-sm text-desktop-sm font-medium transition-colors',
              activeMode === mode.id
                ? 'text-ph-blue-500 border-b-2 border-ph-blue-500 bg-ph-blue-50'
                : 'text-ph-gray-500 hover:text-ph-graphite-500 hover:bg-ph-gray-100'
            )}
            onClick={() => setActiveMode(mode.id)}
            title={mode.description}
          >
            {mode.icon}
            {mode.label}
          </button>
        ))}
      </div>

      {/* Content */}
      <div className="flex-1 p-desktop-md">
        {activeMode === 'chat' && <ChatMode />}
        {activeMode === 'draft' && <DraftMode />}
        {activeMode === 'analyze' && <AnalyzeMode />}
      </div>
    </div>
  );
}

function ChatMode() {
  return (
    <Card>
      <CardContent className="p-desktop-md">
        <div className="space-y-desktop-sm">
          <h3 className="ph-text-desktop-base font-medium">How can I help with healthcare messages?</h3>
          <p className="ph-text-desktop-sm text-ph-gray-500">
            Ask me about HL7, FHIR, NCPDP standards, message validation, or integration troubleshooting.
          </p>
          <div className="mt-desktop-md">
            <textarea
              className="ph-input w-full h-24 resize-none"
              placeholder="Ask about healthcare message standards..."
            />
            <Button className="mt-desktop-sm w-full" variant="primary">
              Send Message
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function DraftMode() {
  return (
    <Card>
      <CardContent className="p-desktop-md">
        <div className="space-y-desktop-sm">
          <h3 className="ph-text-desktop-base font-medium">Intelligent Message Composition</h3>
          <p className="ph-text-desktop-sm text-ph-gray-500">
            Let me help you draft healthcare messages with proper structure and compliance.
          </p>
          <div className="space-y-desktop-sm">
            <select className="ph-input w-full">
              <option>Select Message Type</option>
              <option>HL7 ADT^A01 - Patient Admission</option>
              <option>HL7 ORM^O01 - Order Message</option>
              <option>FHIR Patient Resource</option>
              <option>NCPDP Prescription</option>
            </select>
            <Button className="w-full" variant="primary">
              Generate Draft
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function AnalyzeMode() {
  return (
    <Card>
      <CardContent className="p-desktop-md">
        <div className="space-y-desktop-sm">
          <h3 className="ph-text-desktop-base font-medium">Deep Message Analysis</h3>
          <p className="ph-text-desktop-sm text-ph-gray-500">
            Analyze your messages for PHI, compliance issues, and vendor compatibility.
          </p>
          <div className="space-y-desktop-sm">
            <div className="text-desktop-sm">
              <strong>Available Analysis:</strong>
              <ul className="mt-1 space-y-1 text-ph-gray-500">
                <li>• PHI Detection & Compliance</li>
                <li>• Vendor Pattern Analysis</li>
                <li>• Cross-Standard Validation</li>
                <li>• Integration Compatibility</li>
              </ul>
            </div>
            <Button className="w-full" variant="primary">
              Analyze Current Message
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}