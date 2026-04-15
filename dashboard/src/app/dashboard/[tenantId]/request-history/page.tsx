'use client';
import RequestHistoryPage from '@/page/request-history/RequestHistoryPage';
import { useParams } from 'next/navigation';
import React from 'react';

const Page = () => {
  const params = useParams();
  const tenantId = (params?.tenantId as string) || undefined;
  return <RequestHistoryPage mode="tenant" tenantScope={tenantId} />;
};

export default Page;
