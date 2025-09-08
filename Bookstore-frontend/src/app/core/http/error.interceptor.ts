import { HttpErrorResponse, HttpInterceptorFn } from "@angular/common/http";
import { inject, Injector } from "@angular/core";
import { MatSnackBar } from "@angular/material/snack-bar";
import { catchError } from "rxjs/operators";
import { throwError } from "rxjs";

function extractProblemMessage(err: HttpErrorResponse): string {
  const e = err.error;
  if (e && typeof e === "object" && e.errors && typeof e.errors === "object") {
    const lines: string[] = [];
    for (const [field, arr] of Object.entries(
      e.errors as Record<string, unknown>
    )) {
      const msgs = Array.isArray(arr)
        ? (arr as unknown[]).map((x) => String(x))
        : [String(arr)];
      for (const m of msgs) {
        const niceField = field.replace(/^.*\./, "").replace(/^\$/, "").trim();
        lines.push(`${niceField}: ${m}`);
      }
    }
    if (lines.length) return lines.join("\n");
  }

  if (e?.detail) return e.detail;
  if (e?.message) return e.message;
  if (e?.title) return e.title;

  return err.statusText || `HTTP ${err.status}`;
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const injector = inject(Injector);
  return next(req).pipe(
    catchError((err: unknown) => {
      let message = "Unknown error";
      if (err instanceof HttpErrorResponse) {
        message = extractProblemMessage(err);
      }
      const snack = injector.get(MatSnackBar, null);
      if (snack) {
        snack.open(message, "Close", { duration: 6000 });
      } else {
        console.error("[HTTP ERROR]", message, err);
      }
      return throwError(() => err);
    })
  );
};
