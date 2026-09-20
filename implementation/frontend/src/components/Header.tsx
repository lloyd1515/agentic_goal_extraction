import React from 'react';
import { Sparkles, ShieldCheck, Server, UserCheck } from 'lucide-react';
import { Employee } from '../types/goal';

interface HeaderProps {
  employees: Employee[];
  selectedEmployeeId: string;
  onSelectEmployee: (employeeId: string) => void;
  healthStatus: 'checking' | 'healthy' | 'offline';
  isLoadingEmployees?: boolean;
}

export const Header: React.FC<HeaderProps> = ({
  employees,
  selectedEmployeeId,
  onSelectEmployee,
  healthStatus,
  isLoadingEmployees = false,
}) => {
  return (
    <header className="border-b border-slate-800 bg-[#11141c]/90 backdrop-blur-md sticky top-0 z-40 px-4 md:px-8 py-3.5 flex flex-wrap items-center justify-between gap-4">
      {/* Title and System Badge */}
      <div className="flex items-center space-x-3">
        <div className="p-2 bg-gradient-to-tr from-cyan-500 to-blue-600 rounded-lg shadow-lg shadow-cyan-500/20">
          <Sparkles className="w-5 h-5 text-white" />
        </div>
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-base md:text-lg font-bold tracking-tight text-white">
              Agentic Goal Extraction Canvas
            </h1>
            <span className="text-[10px] font-mono tracking-wider uppercase bg-cyan-950/80 text-cyan-400 border border-cyan-800/60 px-2 py-0.5 rounded font-semibold">
              CQRS + Human-in-the-Loop
            </span>
          </div>
          <p className="text-xs text-slate-400 hidden sm:block">
            Strict Read-Only AI Isolation • Review &amp; Human Approval Gate
          </p>
        </div>
      </div>

      {/* Action Controls & Indicators */}
      <div className="flex items-center flex-wrap gap-3">
        {/* Employee Selector */}
        <div className="flex items-center space-x-2 bg-[#161b26] border border-slate-800 rounded-lg px-2.5 py-1.5 text-xs text-slate-300 focus-within:border-cyan-500/60">
          <UserCheck className="w-3.5 h-3.5 text-cyan-400 shrink-0" />
          <span className="text-slate-400 hidden lg:inline">Employee:</span>
          {isLoadingEmployees ? (
            <span className="text-slate-500 italic">Loading employees...</span>
          ) : (
            <select
              value={selectedEmployeeId}
              onChange={(e) => onSelectEmployee(e.target.value)}
              className="bg-transparent text-slate-100 text-xs font-medium focus:outline-none cursor-pointer max-w-[200px]"
              aria-label="Select Employee Context"
            >
              <option value="" className="bg-[#161b26] text-slate-400">
                -- Select Employee --
              </option>
              {employees.map((emp) => (
                <option key={emp.id} value={emp.id} className="bg-[#161b26] text-slate-200">
                  {emp.fullName} ({emp.department})
                </option>
              ))}
            </select>
          )}
        </div>

        {/* Read-Only AI Badge */}
        <div className="hidden md:flex items-center space-x-2 text-xs bg-[#161b26] border border-slate-800 px-3 py-1.5 rounded-lg">
          <ShieldCheck className="w-4 h-4 text-emerald-400 shrink-0" />
          <span className="text-slate-400">AI Database:</span>
          <span className="font-medium text-emerald-400">Strict Read-Only</span>
        </div>

        {/* API Health Pill */}
        <div className="flex items-center space-x-2 text-xs bg-[#161b26] border border-slate-800 px-3 py-1.5 rounded-lg">
          <Server className="w-3.5 h-3.5 text-cyan-400 shrink-0" />
          <span className="text-slate-400 hidden sm:inline">Backend API:</span>
          <span
            className={`font-semibold capitalize flex items-center gap-1.5 ${
              healthStatus === 'healthy'
                ? 'text-emerald-400'
                : healthStatus === 'checking'
                ? 'text-amber-400'
                : 'text-rose-400'
            }`}
          >
            <span
              className={`w-2 h-2 rounded-full ${
                healthStatus === 'healthy'
                  ? 'bg-emerald-400 animate-pulse'
                  : healthStatus === 'checking'
                  ? 'bg-amber-400 animate-ping'
                  : 'bg-rose-400'
              }`}
            />
            {healthStatus === 'healthy'
              ? 'Healthy'
              : healthStatus === 'checking'
              ? 'Checking...'
              : 'Offline'}
          </span>
        </div>
      </div>
    </header>
  );
};
