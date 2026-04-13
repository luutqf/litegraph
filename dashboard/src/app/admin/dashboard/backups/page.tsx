import BackupPage from '@/page/backups/BackupPage';
import { Metadata } from 'next';
import React from 'react';

export const metadata: Metadata = {
  title: 'LiteGraph | Backups',
  description: 'LiteGraph',
};

const Backups = () => {
  return <BackupPage />;
};

export default Backups;
