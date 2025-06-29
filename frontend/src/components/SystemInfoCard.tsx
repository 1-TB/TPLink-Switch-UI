import { Card, CardContent } from './ui/card';

export default function SystemInfoCard({ info }: { info: any }) {
  return (
    <Card className="mt-4">
      <CardContent className="pt-6">
        <h2 className="text-lg sm:text-xl font-semibold mb-4">System Information</h2>
        <ul className="space-y-3 sm:space-y-2">
          {Object.entries(info).map(([k, v]) => (
            <li key={k} className="flex flex-col sm:flex-row sm:justify-between gap-1 sm:gap-0">
              <span className="font-medium text-muted-foreground capitalize text-sm sm:text-base">
                {k.replace(/([A-Z])/g, ' $1')}:
              </span>
              <span className="text-sm sm:text-base break-all sm:break-normal">{String(v)}</span>
            </li>
          ))}
        </ul>
      </CardContent>
    </Card>
  );
}