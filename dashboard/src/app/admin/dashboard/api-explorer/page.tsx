import ApiExplorerPage from '@/page/api-explorer/ApiExplorerPage';
import { Metadata } from 'next';
import React from 'react';

export const metadata: Metadata = {
  title: 'LiteGraph | API Explorer',
  description: 'LiteGraph',
};

const Page = () => {
  return <ApiExplorerPage />;
};

export default Page;
