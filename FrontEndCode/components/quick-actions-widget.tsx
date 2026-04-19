"use client";

import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import {
  Plus,
  FileText,
  Clock,
  Settings,
  Download,
  Mail,
} from "lucide-react";

interface QuickActionsWidgetProps {
  onAddEmployee?: () => void;
  onCreateRequest?: () => void;
  onScheduleMeeting?: () => void;
}

const actions = [
  {
    icon: Plus,
    label: "Add Employee",
    description: "Hire new staff",
    color: "hover:bg-emerald-50",
    textColor: "text-emerald-600",
  },
  {
    icon: FileText,
    label: "Create Request",
    description: "New approval request",
    color: "hover:bg-blue-50",
    textColor: "text-blue-600",
  },
  {
    icon: Clock,
    label: "Schedule Meeting",
    description: "Team meeting",
    color: "hover:bg-purple-50",
    textColor: "text-purple-600",
  },
  {
    icon: Download,
    label: "Export Report",
    description: "Download data",
    color: "hover:bg-amber-50",
    textColor: "text-amber-600",
  },
  {
    icon: Mail,
    label: "Send Announcement",
    description: "Team message",
    color: "hover:bg-pink-50",
    textColor: "text-pink-600",
  },
  {
    icon: Settings,
    label: "Settings",
    description: "System config",
    color: "hover:bg-slate-50",
    textColor: "text-slate-600",
  },
];

export function QuickActionsWidget({
  onAddEmployee,
  onCreateRequest,
  onScheduleMeeting,
}: QuickActionsWidgetProps) {
  const handlers = [
    onAddEmployee,
    onCreateRequest,
    onScheduleMeeting,
    undefined,
    undefined,
    undefined,
  ];

  return (
    <Card className="p-6 border border-border bg-card">
      <h2 className="text-lg font-semibold text-foreground mb-4">Quick Actions</h2>
      <div className="grid grid-cols-2 gap-3">
        {actions.map((action, index) => {
          const Icon = action.icon;
          const handler = handlers[index];

          return (
            <Button
              key={action.label}
              onClick={handler}
              variant="outline"
              className={`h-auto flex-col items-center justify-center gap-2 p-4 border-border ${action.color}`}
            >
              <Icon size={20} className={action.textColor} />
              <div className="text-center">
                <p className="text-xs font-medium text-foreground">
                  {action.label}
                </p>
                <p className="text-[11px] text-muted-foreground">
                  {action.description}
                </p>
              </div>
            </Button>
          );
        })}
      </div>
    </Card>
  );
}
